namespace BaselinedDocuments
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Linq;

    public class AppData
    {
        public AppData()
        {
            this.ValidateRequiredPaths();
            this.DocsSourceFoldersList = GetDocsSourceFoldersList();
            this.Statements = ReadMappingData();
        }

        public IReadOnlyList<Statement> Statements { get; private set; }

        public string[] DocsSourceFoldersList { get; private set; }


        private IReadOnlyList<Statement> ReadMappingData()
        {
            List<Statement> statements = new List<Statement>();
            string mappingFile = ConfigurationManager.AppSettings.Get("MappingFile");

            if (!File.Exists(mappingFile))
            {
                throw new FileNotFoundException($"Mapping file does not exist at {mappingFile}");
            }

            string[] mappingData = File.ReadAllLines(mappingFile);

            string[] tokens;
            string statName = string.Empty;
            string docTitle = string.Empty;
            string docFileName = string.Empty;

            foreach (var line in mappingData)
            {
                try
                {
                    tokens = line.Split('\t');
                    statName = tokens[0].Trim();
                    docTitle = tokens[2].Trim();
                    docFileName = tokens[3].Trim();
                }
                catch
                {
                    throw new InvalidOperationException($"Map file line \"{line}\" is invalid!");
                }

                if (!statements.Any(s => s.Name.Equals(statName)))
                {
                    var stat = new Statement();
                    stat.Name = statName;
                    statements.Add(stat);
                }


                string docFullName = GetDocumentFullName(docFileName);
                if (string.IsNullOrWhiteSpace(docFullName))
                {
                    throw new ArgumentNullException("DocFullName", $"File \"{docFileName}\" cannot be found");
                }

                Document document = new Document(docTitle, docFileName, docFullName);

                statements.FirstOrDefault(s => s.Name.Equals(statName)).Documents.Add(document);
            }

            return statements;
        }

        private string GetDocumentFullName(string docFileName)
        {
            string docFullName = string.Empty;

            foreach (var folder in this.DocsSourceFoldersList)
            {
                if (File.Exists(Path.Combine(folder, docFileName)))
                {
                    docFullName = Path.Combine(folder, docFileName);
                    break;
                }
            }

            return docFullName;
        }

        private string[] GetDocsSourceFoldersList()
        {
            string sourcePath = ConfigurationManager.AppSettings.Get("SourcePath");
            string sourceFoldersListAsString = ConfigurationManager.AppSettings.Get("SourceFoldersList");

            if (!Directory.Exists(sourcePath))
            {
                throw new DirectoryNotFoundException($"Directory {sourcePath} not found!");
            }

            string[] folderList = sourceFoldersListAsString.Split(';').Select(p => Path.Combine(sourcePath, p.Trim())).ToArray();

            foreach (var folder in folderList)
            {
                if (!Directory.Exists(folder))
                {
                    throw new DirectoryNotFoundException($"Directory {folder} not found!");
                }
            }

            return folderList;
        }

        private void ValidateRequiredPaths()
        {
            if (!Directory.Exists(ConfigurationManager.AppSettings.Get("DestinationPath")))
            {
                throw new DirectoryNotFoundException("Specified directory with existiing archives does not exist!");
            }

            if (!Directory.Exists(ConfigurationManager.AppSettings.Get("NewZipFilesPath")))
            {
                throw new DirectoryNotFoundException("Specified directory for newly created archives does not exist!");
            }
        }
    }
}