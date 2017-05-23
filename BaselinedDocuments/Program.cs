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

            var statements = new List<Statement>();

            GetMappingData(statements);

            CreateFolderStructure(statements, sourcePath, targetPath, baselineFolderName, filesTimeStamp);

            CreateUpdateArchives(targetPath, baselineFolderName);
        }

        private static void GetMappingData(List<Statement> statements)
        {
            string mappingFile = ConfigurationManager.AppSettings.Get("MappingFile");

            if (!File.Exists(mappingFile))
            {
                Console.WriteLine("Mapping file does not exists");
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
        
        private static void CreateFolderStructure(List<Statement> statements, string sourcePath, string targetPath, string baselineFolderName, string filesTimeStamp)
        {
            foreach (var statement in statements)
            {
                string dirName = Path.Combine(targetPath, "Archive - " + statement.Name);
                if (!Directory.Exists(dirName))
                {
                    Directory.CreateDirectory(dirName);
                }
                else
                {
                    Console.WriteLine("Error (folders): This directory already exisits: {0}", dirName);
                }

                string processDir = Path.Combine(dirName, baselineFolderName);
                if (!Directory.Exists(processDir))
                {
                    Directory.CreateDirectory(processDir);
                }
                else
                {
                    Console.WriteLine("Error (folders): This directory already exisits: {0}", processDir);
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
                    }
                }
            }
        }

        private static void CreateUpdateArchives(string workingDir, string baselineFolderName)
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
                        //update exsiting zip
                        Console.WriteLine("Updated " + dir.Name);
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
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Error (zips): " + dir.Name + " : " + e.Message);
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
    }
}
