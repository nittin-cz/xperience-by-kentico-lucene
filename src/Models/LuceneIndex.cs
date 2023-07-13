using System;
using System.Collections.Generic;

using Kentico.Xperience.Lucene.Attributes;

namespace Kentico.Xperience.Lucene.Models
{
    /// <summary>
    /// Represents the configuration of an Lucene index.
    /// </summary>
    public sealed class LuceneIndex
    {
        /// <summary>
        /// The type of the class which extends <see cref="LuceneSearchModel"/>.
        /// </summary>
        public Type Type
        {
            get;
        }


        /// <summary>
        /// The code name of the Lucene index.
        /// </summary>
        public string IndexName
        {
            get;
        }


        /// <summary>
        /// An arbitrary ID used to identify the Lucene index in the admin UI.
        /// </summary>
        internal int Identifier
        {
            get;
            set;
        }


        /// <summary>
        /// The <see cref="IncludedPathAttribute"/>s which are defined in the search model.
        /// </summary>
        internal IEnumerable<IncludedPathAttribute> IncludedPaths
        {
            get;
            set;
        }


        /// <summary>
        /// Initializes a new <see cref="LuceneIndex"/>.
        /// </summary>
        /// <param name="type">The type of the class which extends <see cref="LuceneSearchModel"/>.</param>
        /// <param name="indexName">The code name of the Lucene index.</param>
        /// <exception cref="ArgumentNullException" />
        /// <exception cref="InvalidOperationException" />
        public LuceneIndex(Type type, string indexName)
        {
            if (String.IsNullOrEmpty(indexName))
            {
                throw new ArgumentNullException(nameof(indexName));
            }

            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            if (!typeof(LuceneSearchModel).IsAssignableFrom(type))
            {
                throw new InvalidOperationException($"The search model {type} must extend {nameof(LuceneSearchModel)}.");
            }

            Type = type;
            IndexName = indexName;
        }
    }
}
