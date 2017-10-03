using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BaselinedDocuments
{
    public static class OutputWriter
    {
        public static void DisplayMessage(string message)
        {
            Console.WriteLine(message);
        }

        public static void WriteMessageInLogFile(string message)
        {
            string logFileName = ConfigurationManager.AppSettings.Get("LogFileName");
            File.AppendAllText(logFileName, DateTime.Now + " : " + message + "\r\n");
        }

        public static void DisplayMessageAndAddToLogFile(string message)
        {
            Console.WriteLine(message);
            string logFileName = ConfigurationManager.AppSettings.Get("LogFileName");
            File.AppendAllText(logFileName, DateTime.Now + " : " + message + "\r\n");
        }

    }
}
