using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Index;
using System;

namespace Kentico.Xperience.Lucene.Services
{
    public interface ILuceneIndexService
    {
        T UseWriter<T>(LuceneIndex index, Func<IndexWriter, T> useIndexWriter);
    }
}