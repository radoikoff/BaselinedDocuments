using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinedDocuments
{
    public class OutputWriter : IOutputWriter
    {
        public void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        public void WriteMessageInLogFile(string message)
        {
            string logFileName = ConfigurationManager.AppSettings.Get("LogFileName");
            File.AppendAllText(logFileName, DateTime.Now + " : " + message + Environment.NewLine);
        }

        public void LogMessage(string message)
        {
            DisplayMessage(message);
            WriteMessageInLogFile(message);
        }
    }
}
