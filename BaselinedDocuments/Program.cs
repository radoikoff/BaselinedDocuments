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

            CreateUpdateArchives(targetPath);
        }

        private static void CreateFolderStructure(List<Statement>statements, string sourcePath, string targetPath, string baselineFolderName, string filesTimeStamp)
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

        private static void CreateUpdateArchives(string workingDir)
        {
            var workingDirInfo = new DirectoryInfo(workingDir);
            foreach (var dir in workingDirInfo.GetDirectories())
            {
                string zipName = Path.Combine(workingDir, dir.Name) + ".zip";
                try
                {
                    ZipFile.CreateFromDirectory(dir.FullName, zipName);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error (zips): " + dir.Name + " : " + e.Message);
                }
            }
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
    }
}
