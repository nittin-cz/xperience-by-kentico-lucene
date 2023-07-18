using System;

namespace Kentico.Xperience.Lucene.Attributes
{
    /// <summary>
    /// A property attribute to indicate a search model property is retrievable within Lucene.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RetrievableAttribute : Attribute
    {
    }
}