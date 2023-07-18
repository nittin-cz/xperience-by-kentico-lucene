using CMS.DocumentEngine;
using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    /// <summary>
    /// Default indexing startegy just implements the methods but does not change the data.
    /// </summary>
    /// <typeparam name="TModel"></typeparam>
    public class DefaultIndexingStrategy<TModel> : IIndexingStrategy<TModel> where TModel : LuceneSearchModel
    {
        /// <inheritdoc />
        public virtual Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
        {
            return Task.FromResult(foundValue);
        }

        /// <inheritdoc />
        public virtual Task<TModel> OnIndexingNode(TreeNode node, TModel model)
        {
            return Task.FromResult(model);
        }

        /// <inheritdoc />
        public virtual Task<Document> OnTransformingToLuceneDocument(TreeNode node, TModel model, Document document)
        {
            return Task.FromResult(document);
        }
    }
}
