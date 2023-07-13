using Lucene.Net.Analysis.Standard;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Kentico.Xperience.Lucene.Models;
using CMS.IO;
using Lucene.Net.Store;
using LuceneDirectory = Lucene.Net.Store.Directory;

namespace Kentico.Xperience.Lucene.Services.Implementations
{
    public class LuceneIndexService : ILuceneIndexService
    {
        const LuceneVersion luceneVersion = LuceneVersion.LUCENE_48;

        public T UseWriter<T>(LuceneIndex index, Func<IndexWriter, T> useIndexWriter)
        {
            string indexPath = Path.Combine(Environment.CurrentDirectory, index.IndexName);

            using LuceneDirectory indexDir = FSDirectory.Open(indexPath);
            Analyzer standardAnalyzer = new StandardAnalyzer(luceneVersion);

            //Create an index writer
            IndexWriterConfig indexConfig = new IndexWriterConfig(luceneVersion, standardAnalyzer);
            indexConfig.OpenMode = OpenMode.CREATE_OR_APPEND;                             // create/overwrite index
            using IndexWriter writer = new IndexWriter(indexDir, indexConfig);

            return useIndexWriter(writer);
        }
    }
}
