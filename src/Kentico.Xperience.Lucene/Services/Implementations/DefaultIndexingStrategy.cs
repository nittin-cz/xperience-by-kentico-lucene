using CMS.DocumentEngine;
using Kentico.Xperience.Lucene.Models;
using System.Threading.Tasks;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    /// <summary>
    /// Default indexing startegy just implements the methods but does not change the data.
    /// </summary>
    public class DefaultLuceneIndexingStrategy : ILuceneIndexingStrategy
    {
        /// <inheritdoc />
        public virtual Task<object> OnIndexingProperty(TreeNode node, string propertyName, string usedColumn, object foundValue)
        {
            return Task.FromResult(foundValue);
        }

        /// <inheritdoc />
        public virtual Task<LuceneSearchModel> OnIndexingNode(TreeNode node, LuceneSearchModel model)
        {
            return Task.FromResult(model);
        }

        /// <inheritdoc />
        public virtual bool ShouldIndexNode(TreeNode node)
        {
            return true;
        }
    }
}
