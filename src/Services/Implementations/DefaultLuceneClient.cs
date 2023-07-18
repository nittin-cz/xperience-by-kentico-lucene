using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.Helpers.Caching.Abstractions;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;



namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILuceneClient"/>.
    /// </summary>
    internal class DefaultLuceneClient : ILuceneClient
    {
        private readonly ILuceneIndexService luceneIndexService;
        private readonly ILuceneSearchModelToDocumentMapper luceneSearchModelToDocumentMapper;

        private readonly ICacheAccessor cacheAccessor;
        private readonly IEventLogService eventLogService;
        private readonly IPageRetriever pageRetriever;
        private readonly IProgressiveCache progressiveCache;
        private const string CACHEKEY_CRAWLER = "Lucene|Crawler|{0}";

        internal const string CACHEKEY_STATISTICS = "Lucene|ListIndices";

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultLuceneClient"/> class.
        /// </summary>
        public DefaultLuceneClient(
            ICacheAccessor cacheAccessor,
            IEventLogService eventLogService,
            IPageRetriever pageRetriever,
            IProgressiveCache progressiveCache,
            ILuceneIndexService luceneIndexService,
            ILuceneSearchModelToDocumentMapper luceneSearchModelToDocumentMapper)
        {
            this.cacheAccessor = cacheAccessor;
            this.eventLogService = eventLogService;
            this.pageRetriever = pageRetriever;
            this.progressiveCache = progressiveCache;
            this.luceneIndexService = luceneIndexService;
            this.luceneSearchModelToDocumentMapper = luceneSearchModelToDocumentMapper;

            //// Initialize HttpClient used for crawler requests if a crawler is registered
            //if (IndexStore.Instance.GetAllCrawlers().Any())
            //{
            //    httpClient.BaseAddress = new Uri(BASE_URL);
            //    httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            //    httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {GetBasicAuthentication()}");
            //}
        }

        /// <inheritdoc />
        public Task<int> DeleteRecords(IEnumerable<string> objectIds, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (objectIds == null || !objectIds.Any())
            {
                return Task.FromResult(0);
            }

            return DeleteRecordsInternal(objectIds, indexName, cancellationToken);
        }


        /// <inheritdoc/>
        public async Task<ICollection<LuceneIndexStatisticsViewModel>> GetStatistics(CancellationToken cancellationToken)
        {
            return IndexStore.Instance.GetAllIndexes().Select(i => {
                var statistics = luceneIndexService.UseSearcher(i, s => new LuceneIndexStatisticsViewModel()
                {
                    Name = i.IndexName,
                    Entries = s.IndexReader.NumDocs,
                });
                return statistics;
            }).ToList();
            //return new List<LuceneIndexStatisticsViewModel>();
            //return await progressiveCache.LoadAsync(async (cs, ct) => {
            //    var response = await searchClient.ListIndicesAsync(ct: ct).ConfigureAwait(false);
            //    return response.Items;
            //}, new CacheSettings(20, CACHEKEY_STATISTICS), cancellationToken).ConfigureAwait(false);
        }


        /// <inheritdoc />
        public Task Rebuild(string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            var luceneIndex = IndexStore.Instance.GetIndex(indexName);
            if (luceneIndex == null)
            {
                throw new InvalidOperationException($"The index '{indexName}' is not registered.");
            }

            return RebuildInternal(luceneIndex, cancellationToken);
        }


        /// <inheritdoc />
        public Task<int> UpsertRecords(IEnumerable<LuceneSearchModel> dataObjects, string indexName, CancellationToken cancellationToken)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (dataObjects == null || !dataObjects.Any())
            {
                return Task.FromResult(0);
            }

            return UpsertRecordsInternal(dataObjects, indexName, cancellationToken);
        }

        private async Task<int> DeleteRecordsInternal(IEnumerable<string> objectIds, string indexName, CancellationToken cancellationToken)
        {
            //var searchIndex = await luceneIndexService.InitializeIndex(indexName, cancellationToken);
            var index = IndexStore.Instance.GetIndex(indexName);
            luceneIndexService.UseWriter(index, (writer) =>
            {
                BooleanQuery booleanQuery = new BooleanQuery();
                foreach (string objectId in objectIds)
                {
                    TermQuery termQuery = new TermQuery(new Term(nameof(LuceneSearchModel.ObjectID), objectId));
                    booleanQuery.Add(termQuery, Occur.SHOULD); // Match any of the object IDs
                }
                // todo use batches
                writer.DeleteDocuments(booleanQuery);
                return "OK";
            });

            return 0;
        }


        private async Task RebuildInternal(LuceneIndex luceneIndex, CancellationToken cancellationToken)
        {
            // Clear statistics cache so listing displays updated data after rebuild
            cacheAccessor.Remove(CACHEKEY_STATISTICS);
            
            var indexedNodes = new List<TreeNode>();
            foreach (var includedPathAttribute in luceneIndex.IncludedPaths)
            {
                var nodes = await pageRetriever.RetrieveMultipleAsync(q =>
                {
                    if (includedPathAttribute.ContentTypes.Length > 0)
                    {
                        q.Types(includedPathAttribute.ContentTypes);
                    }

                    q.Path(includedPathAttribute.AliasPath)
                        .PublishedVersion()
                        .WithCoupledColumns();

                    q.AllCultures();
                }, cancellationToken: cancellationToken);

                indexedNodes.AddRange(nodes);
            }

            // TODO: clear Lucene index using DeleteAll

            //var searchIndex = await luceneIndexService.InitializeIndex(luceneIndex.IndexName, cancellationToken);
            //await searchIndex.ClearObjectsAsync(ct: cancellationToken);

            indexedNodes.ForEach(node => LuceneQueueWorker.EnqueueLuceneQueueItem(new LuceneQueueItem(node, LuceneTaskType.CREATE, luceneIndex.IndexName)));
        }

        private async Task<int> UpsertRecordsInternal(IEnumerable<LuceneSearchModel> dataObjects, string indexName, CancellationToken cancellationToken)
        {
            //var searchIndex = await luceneIndexService.InitializeIndex(indexName, cancellationToken);
            var index = IndexStore.Instance.GetIndex(indexName);
            return luceneIndexService.UseWriter(index, (writer) =>
            {
                var count = 0;
                foreach (var dataObject in dataObjects) {
                    // for now all changes are creates, update to be done later
                    // delete old document, there is no upsert nor update in Lucene
                    writer.DeleteDocuments(new Term(nameof(LuceneSearchModel.ObjectID), dataObject.ObjectID));

                    var document = luceneSearchModelToDocumentMapper.MapModelToDocument(index, dataObject);
                    // add new one
                    writer.AddDocument(document);
                    count++;
                }
                return count;
            });
        }
    }
}
