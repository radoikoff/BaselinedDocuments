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

        //public static void DisplayException(string message)
        //{
        //    ConsoleColor currentColor = Console.ForegroundColor;
        //    Console.ForegroundColor = ConsoleColor.Red;
        //    Console.WriteLine(message);
        //    Console.ForegroundColor = currentColor;
        //}

        public static void WriteMessageInLogFile(string message)
        {
            string logFileName = ConfigurationManager.AppSettings.Get("LogFileName");
            File.AppendAllText(logFileName, DateTime.Now + " : " + message + "\r\n");
        }

    }
}
