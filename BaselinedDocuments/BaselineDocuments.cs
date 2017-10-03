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
    class BaselineDocuments
    {
        static void Main()
        {
            var statements = new List<Statement>();
            string sourcePathMaster = ConfigurationManager.AppSettings.Get("SourcePathMaster");
            string sourcePathDraft = ConfigurationManager.AppSettings.Get("SourcePathDraft");
            var sourceDirInfoMaster = new DirectoryInfo(sourcePathMaster);
            var sourceDirInfoDraft = new DirectoryInfo(sourcePathDraft);
            string[] sourceMasterFileNames = sourceDirInfoMaster.GetFiles().Select(f => f.Name).ToArray();
            string[] sourceDraftFileNames = sourceDirInfoDraft.GetFiles().Select(f => f.Name).ToArray();

            if (!IsRequiredPathsValid())
            {
                OutputWriter.DisplayMessageAndAddToLogFile("ERROR: One or more of defined folder paths are invalid");
                return;
            }

            GetMappingData(statements);

            List<string> missingFiles = GetMissingFiles(statements, sourceMasterFileNames, sourceDraftFileNames);
            if (missingFiles.Any())
            {
                OutputWriter.DisplayMessageAndAddToLogFile("ERROR: The following files are missing:\r\n" + string.Join("\r\n", missingFiles));
                return;
            }

            string destinationPath = ConfigurationManager.AppSettings.Get("DestinationPath");
            string newZipFilesPath = ConfigurationManager.AppSettings.Get("NewZipFilesPath");

            foreach (var statement in statements)
            {
                string fullArchiveName;
                string msgTextSuccess;
                string statementNameFixed = string.Join("_", statement.Name.Split(Path.GetInvalidFileNameChars())); //get rid of illegal folder characters
                string archiveName = "Archive - " + statementNameFixed;

                var destinationDirInfo = new DirectoryInfo(destinationPath);
                var existingZipFileNames = destinationDirInfo.GetFiles("*.zip").Select(f => Path.GetFileNameWithoutExtension(f.Name));

                if (existingZipFileNames.Contains(archiveName)) //Update
                {
                    fullArchiveName = Path.Combine(destinationPath, archiveName) + ".zip";
                    msgTextSuccess = archiveName + " updated successfully";
                    CreateUpdateZip(statement, sourcePathMaster, sourcePathDraft, destinationPath, sourceMasterFileNames, sourceDraftFileNames, fullArchiveName, msgTextSuccess);
                }
                else //create
                {
                    fullArchiveName = Path.Combine(destinationPath, newZipFilesPath, archiveName) + ".zip";
                    msgTextSuccess = archiveName + " created successfuly";
                    CreateUpdateZip(statement, sourcePathMaster, sourcePathDraft, destinationPath, sourceMasterFileNames, sourceDraftFileNames, fullArchiveName, msgTextSuccess);
                }
            }
        }

        private static void GetMappingData(List<Statement> statements)
        {
            string mappingFile = ConfigurationManager.AppSettings.Get("MappingFile");

            if (!File.Exists(mappingFile))
            {
                OutputWriter.DisplayMessageAndAddToLogFile("Mapping file does not exist at specified location");
                return;
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
                    tokens = line.Split(',');
                    statName = tokens[0].Trim();
                    docTitle = tokens[2].Trim();
                    docFileName = tokens[3].Trim();
                }
                catch
                {
                    OutputWriter.DisplayMessageAndAddToLogFile($"ERROR: Map line \"{line}\" is invalid");
                    statements.Clear();
                    return;
                }

                if (!statements.Any(s => s.Name.Equals(statName)))
                {
                    var stat = new Statement();
                    stat.Name = statName;
                    statements.Add(stat);
                }

                var document = new Document(docTitle, docFileName);

                statements.FirstOrDefault(s => s.Name.Equals(statName)).Documents.Add(document);
            }
        }

        private static void CreateUpdateZip(Statement statement, string sourcePathMaster, string sourcePathDraft, string destinationPath, string[] sourceMasterFileNames, string[] sourceDraftFileNames, string fullArchiveName, string msgTextSuccess)
        {
            string filesTimeStamp = ConfigurationManager.AppSettings.Get("FilesTimeStamp");
            string baselineFolderName = ConfigurationManager.AppSettings.Get("BaselineFolderName");
            string sourcePath = String.Empty;

            try
            {
                using (ZipArchive archive = ZipFile.Open(fullArchiveName, ZipArchiveMode.Update))
                {
                    bool isCurrentBaselineExists = archive.Entries.Select(e => Path.GetDirectoryName(e.FullName)).Contains(baselineFolderName);
                    if (!isCurrentBaselineExists)
                    {
                        foreach (var document in statement.Documents)
                        {
                            if (sourceMasterFileNames.Contains(document.FileName))
                            {
                                sourcePath = sourcePathMaster;
                            }
                            else if (sourceDraftFileNames.Contains(document.FileName))
                            {
                                sourcePath = sourcePathDraft;
                            }
                            string newDocumentName = string.Join("-", document.Title.Split(Path.GetInvalidFileNameChars())); //remove illegal chars
                            newDocumentName = newDocumentName + " " + filesTimeStamp + Path.GetExtension(document.FileName);
                            archive.CreateEntryFromFile(Path.Combine(sourcePath, document.FileName), Path.Combine(baselineFolderName, newDocumentName));
                        }
                        OutputWriter.DisplayMessageAndAddToLogFile(msgTextSuccess);
                    }
                    else
                    {
                        OutputWriter.DisplayMessageAndAddToLogFile($"ERROR: Folder {baselineFolderName} already exisits in {fullArchiveName}");
                    }
                }
            }
            catch (Exception exception)
            {
                OutputWriter.DisplayMessageAndAddToLogFile($"ERROR: Unable to open or update {fullArchiveName} : {exception.Message}");
            }
        }

        private static List<string> GetMissingFiles(List<Statement> statements, string[] sourceMasterFileNames, string[] sourceDraftFileNames)
        {
            var requiredFileNames = statements.SelectMany(s => s.Documents.Select(d => d.FileName));
            var combinedFileNames = sourceMasterFileNames.Union(sourceDraftFileNames);
            var notFoundFiles = requiredFileNames.Except(combinedFileNames).ToList();
            return notFoundFiles;
        }

        private static bool IsRequiredPathsValid()
        {
            bool masterPathExists = false;
            bool draftPathExists = false;
            bool destinationPathExists = false;
            bool NewZipFilesPathExists = false;

            if (Directory.Exists(ConfigurationManager.AppSettings.Get("SourcePathMaster")))
            {
                masterPathExists = true;
            }
            else
            {
                OutputWriter.DisplayMessageAndAddToLogFile("Specified directory for master files does not exist");
            }

            if (Directory.Exists(ConfigurationManager.AppSettings.Get("SourcePathDraft")))
            {
                draftPathExists = true;
            }
            else
            {
                OutputWriter.DisplayMessageAndAddToLogFile("Specified directory for draft files does not exist");
            }

            if (Directory.Exists(ConfigurationManager.AppSettings.Get("DestinationPath")))
            {
                destinationPathExists = true;
            }
            else
            {
                OutputWriter.DisplayMessageAndAddToLogFile("Specified destination directory does not exist");
            }

            if (Directory.Exists(ConfigurationManager.AppSettings.Get("NewZipFilesPath")))
            {
                NewZipFilesPathExists = true;
            }
            else
            {
                OutputWriter.DisplayMessageAndAddToLogFile("Specified directory for newly created archives does not exist");
            }

            if (masterPathExists && draftPathExists && destinationPathExists && NewZipFilesPathExists)
            {
                return true;
            }
            return false;
        }

    }
}

