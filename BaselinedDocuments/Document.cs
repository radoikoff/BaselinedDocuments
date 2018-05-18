using System;

namespace BaselinedDocuments
{
    public class Document
    {
        public Document(string title, string fileName, string fullFileName)
        {
            this.Title = title;
            this.FileName = fileName;
            this.FullFileName = fullFileName;
        }

        public string Title { get; private set; }

        public string FileName { get; private set; }

        private string fullFileName;

        public string FullFileName
        {
            get { return fullFileName; }
            private set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentNullException("FullFileName");
                }
                fullFileName = value;
            }
        }

    }
}
