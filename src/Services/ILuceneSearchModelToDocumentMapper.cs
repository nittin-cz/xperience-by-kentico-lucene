using Kentico.Xperience.Lucene.Models;
using Lucene.Net.Documents;

namespace Kentico.Xperience.Lucene.Services
{
    public interface ILuceneSearchModelToDocumentMapper
    {
        Document MapModelToDocument(LuceneIndex luceneIndex, LuceneSearchModel model);
    }
}