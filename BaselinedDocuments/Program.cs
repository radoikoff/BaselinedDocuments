using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using System.Configuration;

namespace BaselinedDocuments
{
    class Program
    {
        static void Main()
        {
            string sourcePath = ConfigurationManager.AppSettings.Get("SourcePath");
            string targetPath = ConfigurationManager.AppSettings.Get("TargetPath");
            string newZipFilesPath = ConfigurationManager.AppSettings.Get("NewZipFilesPath");
            string filesTimeStamp = ConfigurationManager.AppSettings.Get("FilesTimeStamp");
            string baselineFolderName = ConfigurationManager.AppSettings.Get("BaselineFolderName");
            var outputLog = new StringBuilder("Output Log : " + DateTime.Now + "\r\n");
            var statements = new List<Statement>();

            GetMappingData(statements, outputLog);

            CreateUpdateArchives(statements, sourcePath, targetPath, newZipFilesPath, baselineFolderName, filesTimeStamp, outputLog);

            File.AppendAllText("Output.txt", outputLog.ToString());
        }

        private static void GetMappingData(List<Statement> statements, StringBuilder outputLog)
        {
            string mappingFile = ConfigurationManager.AppSettings.Get("MappingFile");

            if (!File.Exists(mappingFile))
            {
                Console.WriteLine("Mapping file does not exists");
                outputLog.Append("Mapping file does not exists \r\n");
                return;
            }

            string[] mappingData = File.ReadAllLines(mappingFile);

            foreach (var item in mappingData)
            {
                string statName = item.Split(',').First().Trim();       //to check if data is valid
                string fileName = item.Split(',').Skip(1).First().Trim();

                if (!statements.Any(s => s.Name.Equals(statName)))
                {
                    var stat = new Statement();
                    stat.Name = statName;
                    statements.Add(stat);
                }

                statements.FirstOrDefault(s => s.Name.Equals(statName)).Documents.Add(fileName);
            }
        }

        private static void CreateUpdateArchives(List<Statement> statements, string sourcePath, string DestinationPath, string newZipFilesPath, string baselineFolderName, string filesTimeStamp, StringBuilder outputLog)
        {
            foreach (var statement in statements)
            {
                string statementNameFixed = string.Join("_", statement.Name.Split(Path.GetInvalidFileNameChars())); //get rid of ilegal folder characters
                string archiveName = "Archive - " + statementNameFixed;
                string fullArchiveName = Path.Combine(DestinationPath, archiveName) + ".zip";

                string msgTextSuccess = String.Empty;

                var destinationDirInfo = new DirectoryInfo(DestinationPath);
                var existingZipFileNames = destinationDirInfo.GetFiles("*.zip").Select(f => Path.GetFileNameWithoutExtension(f.Name));

                if (existingZipFileNames.Contains(archiveName))
                {
                    msgTextSuccess = archiveName + " created sucessfully";
                }
                else
                {
                    msgTextSuccess = archiveName + " updated sucessfuly";
                    fullArchiveName = Path.Combine(DestinationPath, newZipFilesPath, archiveName) + ".zip";
                }


                try
                {
                    using (ZipArchive archive = ZipFile.Open(fullArchiveName, ZipArchiveMode.Update))
                    {
                        bool isCurrentBaselineExists = archive.Entries.Select(e => Path.GetDirectoryName(e.FullName)).Contains(baselineFolderName);
                        if (!isCurrentBaselineExists)
                        {
                            foreach (var document in statement.Documents)
                            {
                                string newDocumentName = Path.GetFileNameWithoutExtension(document) + " " + filesTimeStamp + Path.GetExtension(document);
                                archive.CreateEntryFromFile(Path.Combine(sourcePath, document), Path.Combine(baselineFolderName, newDocumentName));
                            }
                            Console.WriteLine(msgTextSuccess);
                            outputLog.Append(msgTextSuccess + "\r\n");
                        }
                        else
                        {
                            Console.WriteLine($"ERROR: Folder {baselineFolderName} already exisits in {archiveName}");
                            outputLog.Append($"ERROR: Folder {baselineFolderName} already exisits in {archiveName} \r\n");
                        }
                    }
                }
                catch (Exception exception)
                {
                    Console.WriteLine($"ERROR: Unable to open or update {fullArchiveName} : {exception.Message}");
                    outputLog.Append($"ERROR: Unable to open or update {fullArchiveName} : {exception.Message} \r\n");
                }
            }

        }
    }
}

