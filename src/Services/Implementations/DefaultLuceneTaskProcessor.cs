using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.WorkflowEngine;

using Kentico.Xperience.Lucene.Models;

using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Lucene.Services
{
    internal class DefaultLuceneTaskProcessor : ILuceneTaskProcessor
    {
        private readonly ILuceneClient luceneClient;
        private readonly ILuceneObjectGenerator luceneObjectGenerator;
        private readonly IEventLogService eventLogService;
        private readonly IWorkflowStepInfoProvider workflowStepInfoProvider;
        private readonly IVersionHistoryInfoProvider versionHistoryInfoProvider;


        public DefaultLuceneTaskProcessor(ILuceneClient luceneClient,
            IEventLogService eventLogService,
            IWorkflowStepInfoProvider workflowStepInfoProvider,
            IVersionHistoryInfoProvider versionHistoryInfoProvider,
            ILuceneObjectGenerator luceneObjectGenerator)
        {
            this.luceneClient = luceneClient;
            this.eventLogService = eventLogService;
            this.workflowStepInfoProvider = workflowStepInfoProvider;
            this.versionHistoryInfoProvider = versionHistoryInfoProvider;
            this.luceneObjectGenerator = luceneObjectGenerator;
        }


        /// <inheritdoc />
        public async Task<int> ProcessLuceneTasks(IEnumerable<LuceneQueueItem> queueItems, CancellationToken cancellationToken)
        {
            var successfulOperations = 0;

            // Group queue items based on index name
            var groups = queueItems.GroupBy(item => item.IndexName);
            foreach (var group in groups)
            {
                try
                {
                    var luceneIndex = IndexStore.Instance.GetIndex(group.Key);

                    var deleteIds = new List<string>();
                    var deleteTasks = group.Where(queueItem => queueItem.TaskType == LuceneTaskType.DELETE);
                    deleteIds.AddRange(GetIdsToDelete(luceneIndex, deleteTasks));

                    var updateTasks = group.Where(queueItem => queueItem.TaskType == LuceneTaskType.UPDATE || queueItem.TaskType == LuceneTaskType.CREATE);
                    var upsertData = new List<LuceneSearchModel>();
                    foreach (var queueItem in updateTasks)
                    {
                        var data = await luceneObjectGenerator.GetTreeNodeData(queueItem);
                        upsertData.Add(data);
                    }

                    successfulOperations += await luceneClient.DeleteRecords(deleteIds, group.Key, cancellationToken);
                    successfulOperations += await luceneClient.UpsertRecords(upsertData, group.Key, cancellationToken);
                }
                catch (Exception ex)
                {
                    eventLogService.LogError(nameof(DefaultLuceneTaskProcessor), nameof(ProcessLuceneTasks), ex.Message);
                }
            }

            return successfulOperations;
        }

        private IEnumerable<string> GetIdsToDelete(LuceneIndex luceneIndex, IEnumerable<LuceneQueueItem> deleteTasks)
        {
            return deleteTasks.Select(queueItem => queueItem.Node.DocumentID.ToString());
        }
    }
}
