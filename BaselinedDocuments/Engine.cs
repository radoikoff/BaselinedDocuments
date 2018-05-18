using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinedDocuments
{
    public class Engine
    {
        private IOutputWriter logger;
        private AppData data;

        public Engine(IOutputWriter logger, AppData data)
        {
            this.logger = logger;
            this.data = data;
        }

        public void ProcessZips()
        {
            string destinationPath = ConfigurationManager.AppSettings.Get("DestinationPath");
            string newZipFilesPath = ConfigurationManager.AppSettings.Get("NewZipFilesPath");

            foreach (var statement in data.Statements)
            {
                string fullArchiveName;
                string statementNameFixed = string.Join("_", statement.Name.Split(Path.GetInvalidFileNameChars())); //get rid of illegal folder characters
                string archiveName = "Archive - " + statementNameFixed;

                var destinationDirInfo = new DirectoryInfo(destinationPath);
                var existingZipFileNames = destinationDirInfo.GetFiles("*.zip").Select(f => Path.GetFileNameWithoutExtension(f.Name));

                if (existingZipFileNames.Contains(archiveName)) //Update
                {
                    fullArchiveName = Path.Combine(destinationPath, archiveName) + ".zip";
                    bool success = CreateUpdateZip(statement, destinationPath, fullArchiveName);
                    if (success) logger.LogMessage(archiveName + " updated successfully");
                }
                else //create
                {
                    fullArchiveName = Path.Combine(destinationPath, newZipFilesPath, archiveName) + ".zip";
                    bool success = CreateUpdateZip(statement, destinationPath, fullArchiveName);
                    if (success) logger.LogMessage(archiveName + " created successfuly");
                }
            }
        }

        private bool CreateUpdateZip(Statement statement, string destinationPath, string fullArchiveName)
        {
            string filesTimeStamp = ConfigurationManager.AppSettings.Get("FilesTimeStamp");
            string baselineFolderName = ConfigurationManager.AppSettings.Get("BaselineFolderName");

            try
            {
                using (ZipArchive archive = ZipFile.Open(fullArchiveName, ZipArchiveMode.Update))
                {
                    bool isCurrentBaselineExists = archive.Entries.Select(e => Path.GetDirectoryName(e.FullName)).Contains(baselineFolderName);
                    if (isCurrentBaselineExists)
                    {
                        this.logger.LogMessage($"ERROR: Folder {baselineFolderName} already exisits in {fullArchiveName}");
                        return false;
                    }

                    foreach (var document in statement.Documents)
                    {
                        string newDocumentName = string.Join("-", document.Title.Split(Path.GetInvalidFileNameChars())); //remove illegal chars
                        newDocumentName = newDocumentName + " " + filesTimeStamp + Path.GetExtension(document.FileName);
                        archive.CreateEntryFromFile(document.FullFileName, Path.Combine(baselineFolderName, newDocumentName));
                    }
                }
            }
            catch (Exception exception)
            {
                this.logger.LogMessage($"ERROR: Unable to open or update {fullArchiveName} : {exception.Message}");
                return false;
            }

            return true;
        }
    }
}
