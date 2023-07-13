using Lucene.Net.Documents;
using Lucene.Net.Index;
using System.Collections.Generic;

namespace Kentico.Xperience.Lucene.Models
{
    public class LuceneDocument
    {
        public string ObjectID { get; set; }
        public LuceneTaskType TaskType { get; set; }
        public Document Document { get; set; }
    }
}
