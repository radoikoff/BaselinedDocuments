namespace BaselinedDocuments
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Text;

    public class AppData
    {
        public AppData()
        {
            this.ValidateRequiredPaths();
            this.DocsSourceFoldersList = GetDocsSourceFoldersList();
            this.Statements = GetStatements();
        }

        public IReadOnlyList<Statement> Statements { get; private set; }

        public string[] DocsSourceFoldersList { get; private set; }


        private IReadOnlyList<Statement> GetStatements()
        {
            string mode = ConfigurationManager.AppSettings.Get("InputMode");
            if (mode == "SQL")
            {
                DataTable data = GetSqlMappingData();
                CreateMappingFile(data);
                return ReadMappingData(data);
            }
            else if (mode == "TXT")
            {
                return ReadMappingData(GetTextMappingData());
            }
            else
            {
                throw new InvalidOperationException($"Provided input mode setting \"{mode}\" is invalid!");
            }
        }

        private IReadOnlyList<Statement> ReadMappingData(string[] mappingData)
        {
            List<Statement> statements = new List<Statement>();

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

                ProcessSingleDataRow(statName, docTitle, docFileName, statements);
            }

            return statements;
        }

        private IReadOnlyList<Statement> ReadMappingData(DataTable mappingData)
        {
            List<Statement> statements = new List<Statement>();
            foreach (DataRow row in mappingData.Rows)
            {
                string statName = row[0].ToString();
                string docTitle = row[2].ToString();
                string docFileName = row[3].ToString();

                ProcessSingleDataRow(statName, docTitle, docFileName, statements);
            }
            return statements;
        }

        private void ProcessSingleDataRow(string statName, string docTitle, string docFileName, List<Statement> statements)
        {
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

        private DataTable GetSqlMappingData()
        {
            DataTable dataTable = new DataTable();
            using (SqlConnection sqlConnection = new SqlConnection(ConfigurationManager.ConnectionStrings["DB"].ConnectionString))
            {
                using (SqlCommand cmd = new SqlCommand("[dbo].[GetControlledDocumentsPerStatement]", sqlConnection))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    using (SqlDataAdapter da = new SqlDataAdapter(cmd))
                    {
                        da.Fill(dataTable);
                    }
                }
            }
            return dataTable;
        }

        private string[] GetTextMappingData()
        {
            string mappingFile = ConfigurationManager.AppSettings.Get("MappingFile");

            if (!File.Exists(mappingFile))
            {
                throw new FileNotFoundException($"Mapping file does not exist at {mappingFile}");
            }

            string[] mappingData = File.ReadAllLines(mappingFile);
            return mappingData;
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
            string[] folderList = ConfigurationManager.AppSettings.AllKeys
                                    .Where(key => key.StartsWith("SourceFolder_"))
                                    .Select(key => ConfigurationManager.AppSettings[key])
                                    .ToArray();

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

        private void CreateMappingFile(DataTable data)
        {
            string path = ConfigurationManager.AppSettings.Get("MappingFile");
            StringBuilder sb = new StringBuilder();

            sb.AppendLine(string.Join(", ", data.Columns.Cast<DataColumn>().Select(c => c.ColumnName)));

            foreach (DataRow row in data.Rows)
            {
                sb.AppendLine(string.Join(", ", row.ItemArray));

            }

            File.WriteAllText(path, sb.ToString());
        }

    }
}