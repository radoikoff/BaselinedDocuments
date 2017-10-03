using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinedDocuments
{
    public class Document
    {

        public Document(string title, string fileName)
        {
            this.Title = title;
            this.FileName = fileName;
        }

        public string Title { get; set; }

        public string FileName { get; set; }
    }
}
