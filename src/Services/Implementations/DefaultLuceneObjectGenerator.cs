using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using CMS.Core;
using CMS.DataEngine;
using CMS.DocumentEngine;
using CMS.FormEngine;
using CMS.MediaLibrary;

using Kentico.Content.Web.Mvc;
using Kentico.Xperience.Lucene.Attributes;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Kentico.Xperience.Lucene.Services
{
    /// <summary>
    /// Default implementation of <see cref="ILuceneObjectGenerator"/>.
    /// </summary>
    internal class DefaultLuceneObjectGenerator : ILuceneObjectGenerator
    {
        private readonly IConversionService conversionService;
        private readonly IEventLogService eventLogService;
        private readonly IMediaFileInfoProvider mediaFileInfoProvider;
        private readonly IMediaFileUrlRetriever mediaFileUrlRetriever;
        private readonly Dictionary<string, string[]> cachedIndexedColumns = new();
        private readonly string[] ignoredPropertiesForTrackingChanges = new string[] {
            nameof(LuceneSearchModel.ObjectID),
            nameof(LuceneSearchModel.Url),
            nameof(LuceneSearchModel.ClassName)
        };


        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultLuceneObjectGenerator"/> class.
        /// </summary>
        public DefaultLuceneObjectGenerator(IConversionService conversionService,
            IEventLogService eventLogService,
            IMediaFileInfoProvider mediaFileInfoProvider,
            IMediaFileUrlRetriever mediaFileUrlRetriever)
        {
            this.conversionService = conversionService;
            this.eventLogService = eventLogService;
            this.mediaFileInfoProvider = mediaFileInfoProvider;
            this.mediaFileUrlRetriever = mediaFileUrlRetriever;
        }


        /// <inheritdoc/>
        public LuceneDocument GetTreeNodeData(LuceneQueueItem queueItem)
        {
            var data = new LuceneDocument() {
                Document = new Document(),
            };
            MapChangedProperties(queueItem, data);
            MapCommonProperties(queueItem.Node, data);

            return data;
        }

        /// <summary>
        /// Converts the assets from the <paramref name="node"/>'s value into absolute URLs.
        /// </summary>
        /// <remarks>Logs an error if the definition of the <paramref name="columnName"/> can't
        /// be found.</remarks>
        /// <param name="node">The <see cref="TreeNode"/> the value was loaded from.</param>
        /// <param name="nodeValue">The original value of the column.</param>
        /// <param name="columnName">The name of the column the value was loaded from.</param>
        /// <returns>An list of absolute URLs, or an empty list.</returns>
        private IEnumerable<string> GetAssetUrlsForColumn(TreeNode node, object nodeValue, string columnName)
        {
            var strValue = conversionService.GetString(nodeValue, String.Empty);
            if (String.IsNullOrEmpty(strValue))
            {
                return Enumerable.Empty<string>();
            }

            // Ensure field is Asset type
            var dataClassInfo = DataClassInfoProvider.GetDataClassInfo(node.ClassName, false);
            var formInfo = new FormInfo(dataClassInfo.ClassFormDefinition);
            var field = formInfo.GetFormField(columnName);
            if (field == null)
            {
                eventLogService.LogError(nameof(DefaultLuceneObjectGenerator), nameof(GetAssetUrlsForColumn), $"Unable to load field definition for content type '{node.ClassName}' column name '{columnName}.'");
                return Enumerable.Empty<string>();
            }

            if (!field.DataType.Equals(FieldDataType.Assets, StringComparison.OrdinalIgnoreCase))
            {
                return Enumerable.Empty<string>();
            }

            var dataType = DataTypeManager.GetDataType(typeof(IEnumerable<AssetRelatedItem>));
            if (dataType.Convert(strValue, null, null) is not IEnumerable<AssetRelatedItem> assets)
            {
                return Enumerable.Empty<string>();
            }

            var mediaFiles = mediaFileInfoProvider.Get().ForAssets(assets);

            return mediaFiles.Select(file => mediaFileUrlRetriever.Retrieve(file).RelativePath);
        }


        /// <summary>
        /// Gets the names of all database columns which are indexed by the passed index,
        /// minus those listed in <see cref="ignoredPropertiesForTrackingChanges"/>.
        /// </summary>
        /// <param name="indexName">The index to load columns for.</param>
        /// <returns>The database columns that are indexed.</returns>
        private string[] GetIndexedColumnNames(string indexName)
        {
            if (cachedIndexedColumns.TryGetValue(indexName, out string[] value))
            {
                return value;
            }

            // Don't include properties with SourceAttribute at first, check the sources and add to list after
            var luceneIndex = IndexStore.Instance.GetIndex(indexName);
            var indexedColumnNames = luceneIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(prop => !Attribute.IsDefined(prop, typeof(SourceAttribute))).Select(prop => prop.Name).ToList();
            var propertiesWithSourceAttribute = luceneIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(prop => Attribute.IsDefined(prop, typeof(SourceAttribute)));
            foreach (var property in propertiesWithSourceAttribute)
            {
                var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
                if (sourceAttribute == null)
                {
                    continue;
                }

                indexedColumnNames.AddRange(sourceAttribute.Sources);
            }

            // Remove column names from LuceneSearchModel that aren't database columns
            indexedColumnNames.RemoveAll(col => ignoredPropertiesForTrackingChanges.Contains(col));

            var indexedColumns = indexedColumnNames.ToArray();
            cachedIndexedColumns.Add(indexName, indexedColumns);

            return indexedColumns;
        }


        /// <summary>
        /// Gets the <paramref name="node"/> value using the <paramref name="property"/>
        /// name, or the property's <see cref="SourceAttribute"/> if specified.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load a value from.</param>
        /// <param name="property">The Lucene search model property.</param>
        /// <param name="searchModelType">The Lucene search model.</param>
        /// <param name="columnsToUpdate">A list of columns to retrieve values for. Columns not present
        /// in this list will return <c>null</c>.</param>
        private object GetNodeValue(TreeNode node, PropertyInfo property, Type searchModelType, IEnumerable<string> columnsToUpdate)
        {
            object nodeValue = null;
            var usedColumn = property.Name;
            if (Attribute.IsDefined(property, typeof(SourceAttribute)))
            {
                // Property uses SourceAttribute, loop through column names until a non-null value is found
                var sourceAttribute = property.GetCustomAttributes<SourceAttribute>(false).FirstOrDefault();
                foreach (var source in sourceAttribute.Sources.Where(s => columnsToUpdate.Contains(s)))
                {
                    nodeValue = node.GetValue(source);
                    if (nodeValue != null)
                    {
                        usedColumn = source;
                        break;
                    }
                }
            }
            else
            {
                if (!columnsToUpdate.Contains(property.Name))
                {
                    return null;
                }

                nodeValue = node.GetValue(property.Name);
            }

            // Convert node value to URLs if necessary
            if (nodeValue != null && Attribute.IsDefined(property, typeof(MediaUrlsAttribute)))
            {
                nodeValue = GetAssetUrlsForColumn(node, nodeValue, usedColumn);
            }

            var searchModel = Activator.CreateInstance(searchModelType) as LuceneSearchModel;
            nodeValue = searchModel.OnIndexingProperty(node, property.Name, usedColumn, nodeValue);

            return nodeValue;
        }


        /// <summary>
        /// Adds values to the <paramref name="data"/> by retriving the indexed columns of the index
        /// and getting values from the <see cref="LuceneQueueItem.Node"/>. When the <see cref="LuceneQueueItem.TaskType"/>
        /// is <see cref="LuceneTaskType.UPDATE"/>, only the <see cref="LuceneQueueItem.ChangedColumns"/>
        /// will be added to the <paramref name="data"/>.
        /// </summary>
        private void MapChangedProperties(LuceneQueueItem queueItem, LuceneDocument data)
        {
            var serializer = new JsonSerializer();
            serializer.Converters.Add(new DecimalPrecisionConverter());

            var columnsToUpdate = new List<string>();
            var indexedColumns = GetIndexedColumnNames(queueItem.IndexName);
            if (queueItem.TaskType == LuceneTaskType.CREATE)
            {
                columnsToUpdate.AddRange(indexedColumns);
            }
            else if (queueItem.TaskType == LuceneTaskType.UPDATE)
            {
                columnsToUpdate.AddRange(queueItem.ChangedColumns.Intersect(indexedColumns));
            }

            var luceneIndex = IndexStore.Instance.GetIndex(queueItem.IndexName);
            var properties = luceneIndex.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in properties)
            {
                object nodeValue = GetNodeValue(queueItem.Node, prop, luceneIndex.Type, columnsToUpdate);
                if (nodeValue == null)
                {
                    continue;
                }

                // TODO: consider inheriting field type from prop type
                // assign field type
                if (Attribute.IsDefined(prop, typeof(TextFieldAttribute)))
                {
                    var textFieldAttribute = prop.GetCustomAttributes<TextFieldAttribute>(false).FirstOrDefault();
                    data.Document.Add(new TextField(prop.Name, nodeValue.ToString(), textFieldAttribute.Store ? Field.Store.YES : Field.Store.NO));
                }
                if (Attribute.IsDefined(prop, typeof(StringFieldAttribute)))
                {
                    var stringFieldAttribute = prop.GetCustomAttributes<StringFieldAttribute>(false).FirstOrDefault();
                    data.Document.Add(new TextField(prop.Name, nodeValue.ToString(), stringFieldAttribute.Store ? Field.Store.YES : Field.Store.NO));
                }
                else if (Attribute.IsDefined(prop, typeof(Int32FieldAttribute)))
                {
                    var intFieldAttribute = prop.GetCustomAttributes<Int32FieldAttribute>(false).FirstOrDefault();
                    data.Document.Add(new Int32Field(prop.Name, (int)nodeValue, intFieldAttribute.Store ? Field.Store.YES : Field.Store.NO));
                } else { 
                    // TODO: log some warning or implement default to text field
                }
            }
        }


        /// <summary>
        /// Sets values in the <paramref name="data"/> object using the common search model properties
        /// located within the <see cref="LuceneSearchModel"/> class.
        /// </summary>
        /// <param name="node">The <see cref="TreeNode"/> to load values from.</param>
        /// <param name="data">The dynamic data that will be passed to Lucene.</param>
        private static void MapCommonProperties(TreeNode node, LuceneDocument data)
        {
            data.ObjectID = node.DocumentID.ToString();
            data.Document.Add(new StringField(nameof(LuceneSearchModel.ObjectID), data.ObjectID, Field.Store.YES));
            data.Document.Add(new StringField(nameof(LuceneSearchModel.ClassName), node.ClassName, Field.Store.YES));

            string url;
            try
            {
                url = DocumentURLProvider.GetAbsoluteUrl(node);
            }
            catch (Exception)
            {
                // GetAbsoluteUrl can throw an exception when processing a page update LuceneQueueItem
                // and the page was deleted before the update task has processed. In this case, upsert an
                // empty URL
                url = String.Empty;
            }

            data.Document.Add(new StringField(nameof(LuceneSearchModel.Url), url, Field.Store.YES));
        }
    }
}
