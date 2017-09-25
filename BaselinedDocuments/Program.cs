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
            string filesTimeStamp = ConfigurationManager.AppSettings.Get("FilesTimeStamp");
            string baselineFolderName = ConfigurationManager.AppSettings.Get("BaselineFolderName");
            var outputLog = new StringBuilder("Output Log : " + DateTime.Now + "\r\n");
            var statements = new List<Statement>();

            GetMappingData(statements, outputLog);

            Test(statements, sourcePath, targetPath, baselineFolderName, filesTimeStamp, outputLog);

            // CreateFolderStructure(statements, sourcePath, targetPath, baselineFolderName, filesTimeStamp, outputLog);

            // CreateUpdateArchives(targetPath, baselineFolderName, outputLog);

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

        private static void CreateFolderStructure(List<Statement> statements, string sourcePath, string targetPath, string baselineFolderName, string filesTimeStamp, StringBuilder outputLog)
        {
            foreach (var statement in statements)
            {
                string statementNameFixed = string.Join("_", statement.Name.Split(Path.GetInvalidFileNameChars())); //get rid of ilegal folder characters
                string dirName = Path.Combine(targetPath, "Archive - " + statementNameFixed);

                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                else
                {
                    Console.WriteLine("Error (folders): This directory already exisits: {0}", dirName);
                    outputLog.Append($"Error (folders): This directory already exisits: {dirName} \r\n");
                }

                string processDir = Path.Combine(dirName, baselineFolderName);
                if (!Directory.Exists(processDir))
                {
                    Directory.CreateDirectory(processDir);
                }
                else
                {
                    Console.WriteLine("Error (folders): This directory already exisits: {0}", processDir);
                    outputLog.Append($"Error (folders): This directory already exisits: {processDir} \r\n");
                }

                foreach (var document in statement.Documents)
                {
                    var newDocumentName = Path.GetFileNameWithoutExtension(document) + " " + filesTimeStamp + Path.GetExtension(document);
                    string sourceFile = Path.Combine(sourcePath, document);
                    string destFile = Path.Combine(processDir, newDocumentName);

                    try
                    {
                        File.Copy(sourceFile, destFile);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error (files): " + statement.Name + " : " + e.Message);
                        outputLog.Append("Error (files): " + statement.Name + " : " + e.Message + "\r\n");
                    }
                }
            }
        }

        private static void CreateUpdateArchives(string workingDir, string baselineFolderName, StringBuilder outputLog)
        {
            var workingDirInfo = new DirectoryInfo(workingDir);
            var zipFiles = workingDirInfo.GetFiles("*.zip").Select(f => Path.GetFileNameWithoutExtension(f.Name));

            foreach (var dir in workingDirInfo.GetDirectories())
            {
                if (zipFiles.Contains(dir.Name))
                {
                    ZipArchive archive = ZipFile.OpenRead(Path.Combine(workingDir, dir.Name + ".zip"));
                    bool isCurrentBaselineExists = archive.Entries.Select(e => Path.GetDirectoryName(e.FullName)).Contains(baselineFolderName);
                    archive.Dispose();

                    if (!isCurrentBaselineExists)
                    {
                        ZipArchive archiveToUpdate = ZipFile.Open(Path.Combine(workingDir, dir.Name + ".zip"), ZipArchiveMode.Update);
                        archiveToUpdate.CreateEntry(baselineFolderName + "//");

                        //update exsiting zip
                        Console.WriteLine("Updated " + dir.Name);
                        outputLog.Append("Updated " + dir.Name + "\r\n");
                    }
                    else
                    {
                        //curent baseline folder exisits err msg.
                    }
                }
                else
                {
                    //create new zip
                    string zipName = Path.Combine(workingDir, dir.Name) + ".zip";
                    try
                    {
                        ZipFile.CreateFromDirectory(dir.FullName, zipName);
                        Console.WriteLine("Created " + zipName);
                        outputLog.Append("Created " + zipName + "\r\n");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error (zips): " + dir.Name + " : " + e.Message);
                        outputLog.Append("Error (zips): " + dir.Name + " : " + e.Message + "\r\n");
                    }
                }
                DeleteDirectoryContent(dir.FullName);
                Directory.Delete(dir.FullName);
            }
        }

        private static void DeleteDirectoryContent(string directoryFullName)
        {
            var directory = new DirectoryInfo(directoryFullName);

            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }

            foreach (var dir in directory.GetDirectories())
            {
                DeleteDirectoryContent(dir.FullName);
                dir.Delete();
            }
        }


        private static void Test(List<Statement> statements, string sourcePath, string DestinationPath, string baselineFolderName, string filesTimeStamp, StringBuilder outputLog)
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
                    msgTextSuccess = archiveName + "created sucessfully";
                }
                else
                {
                    msgTextSuccess = archiveName + "updated sucessfuly";
                    fullArchiveName = Path.Combine(DestinationPath, "NewZips", archiveName) + ".zip";
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
                                archive.CreateEntryFromFile(Path.Combine(sourcePath, document), Path.Combine(baselineFolderName, document));
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

