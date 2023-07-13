using System;

namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute to indicate a search model property is searchable within Lucene.
    /// </summary>
   [AttributeUsage(AttributeTargets.Property)]
    public sealed class SearchableAttribute : Attribute
    {
        /// <summary>
        /// A non-negative number indicating the priority of the attribute when searching, where
        /// zero is the highest priority. Lucene records with matching search terms in high priority
        /// attributes will appear higher in the search results than records with matches in lower
        /// priority attributes.
        /// </summary>
        public int Order
        {
            get;
            set;
        } = -1;


        /// <summary>
        /// If true, a search term match anywhere in the attribute's value has the same weight. If
        /// false, matching terms near the begininng of the value have higher weight than matches
        /// near the end.
        /// </summary>
        public bool Unordered
        {
            get;
            set;
        }
    }
}