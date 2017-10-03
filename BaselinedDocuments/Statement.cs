using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinedDocuments
{
    public class Statement
    {
        public Statement()
        {
            this.Name = String.Empty;
            this.Documents = new HashSet<Document>();
        }

        public string Name { get; set; }

        public HashSet<Document> Documents { get; set; }
    }
}
