using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.IO;

namespace org.ohdsi.cdm.presentation.builder
{
    public class FileLogger
    {
        private string path;

        private static BlockingCollection<string> queue=new BlockingCollection<string>();

        public FileLogger()
        {
            path = AppDomain.CurrentDomain.BaseDirectory + "\\log"; 

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            path = path + "\\" + Settings.Current.Building.LogFilePath;

            if (!File.Exists(path))
            {
                // Create a file to write to.
                using StreamWriter sw = File.CreateText(path);
                sw.WriteLine(
                    "-- ------------------------------------ NEW LOG FILE -------------------------------- --");
            }
        }

        private void DoLog(string cnt)
        {
            if (path.Length <= 0)
            {
                return;
            }

            if (!IsFileLocked())
            {
                // This text is always added, making the file longer over time
                // if it is not deleted.
                try {
                    using StreamWriter sw = File.AppendText(path);
                    var str = DateTime.Now.ToString("s", DateTimeFormatInfo.InvariantInfo);
                    sw.WriteLine($"{str} : {cnt}");
                }
                catch(Exception) { }
            }
        }

        protected virtual bool IsFileLocked()
        {
            try
            {
                using (FileStream stream = (new FileInfo(path)).Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                {
                    stream.Close();
                }
            }
            catch (IOException)
            {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }

        public static void WriteLog(string content)
        {
            (new FileLogger()).DoLog(content);
        }

        public static void Console(string content) {
            WriteLog(content);
            System.Console.WriteLine(content);
        }
    }
}