using System;
using System.Globalization;
using System.IO;

namespace org.ohdsi.cdm.presentation.builder
{
    public class FileLogger
    {
        private string path;

        public FileLogger()
        {
            path = AppDomain.CurrentDomain.BaseDirectory + "\\log"; 
        }

        private void DoLog(string cnt)
        {
            if (path.Length <= 0)
            {
                return;
            }

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "\\" + DateTime.Now.ToString("yyyyMMdd")+ Settings.Current.Building.LogFilePath;

            if (!File.Exists(path))
            {
                // Create a file to write to.
                using StreamWriter sw = File.CreateText(path);
                sw.WriteLine(
                    "-- ------------------------------------ NEW LOG FILE -------------------------------- --");
            }

            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(path))
            {
                var str = DateTime.Now.ToString("s", DateTimeFormatInfo.InvariantInfo);
                sw.WriteLine($"{str} : {cnt}");
            }
        }

        public static void WriteLog(string content)
        {
            (new FileLogger()).DoLog(content);
        }
    }
}