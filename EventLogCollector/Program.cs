using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;

namespace EventLogCollector
{
    class Program
    {
        public static readonly string virtualPath = "C:\\Windows\\Sysnative\\winevt\\Logs\\";
        public static readonly string actualPath = "C:\\Windows\\System32\\winevt\\Logs\\";
        public static readonly string queryString = "*[System[(Level < 5) and TimeCreated[timediff(@SystemTime) < 3600000]]]";

        static void Main(string[] args)
        {
            var dirInfo = new DirectoryInfo(virtualPath);
            var fileList = dirInfo.GetFiles();

            FileStream outputStream = null;
            StreamWriter outputWriter = null;

            try
            {
                outputStream = new FileStream("Events.xml", FileMode.Create, FileAccess.ReadWrite);
                outputWriter = new StreamWriter(outputStream);
                outputWriter.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                outputWriter.WriteLine("<Logs>");

                foreach (var logFile in fileList)
                {
                    EventLogQuery logQuery = null;
                    EventLogReader logReader = null;
                    EventRecord logRecord = null;
                    List<string> xmlRecords = null;

                    try
                    {
                        logQuery = new EventLogQuery(string.Format("{0}{1}", actualPath, logFile.Name), PathType.FilePath, queryString);
                        logReader = new EventLogReader(logQuery);
                        xmlRecords = new List<string>();

                        while ((logRecord = logReader.ReadEvent()) != null)
                            xmlRecords.Add(logRecord.ToXml());
                    }
                    catch (UnauthorizedAccessException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(ex.GetType());
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(": {1}", ex.GetType(), ex.Message);

                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.White;
                        Console.WriteLine("This program requires administrative rights in order to function.", ex.Message);
                        Console.WriteLine("Please right click on the executable and select Run as Administrator.", ex.Message);
                        Console.ForegroundColor = ConsoleColor.Gray;

                        break;
                    }
                    catch (EventLogException ex)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                        Console.Write(ex.GetType());
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine(": {1}", ex.GetType(), ex.Message);
                        Console.ForegroundColor = ConsoleColor.Gray;
                    }
                    finally
                    {
                        if (logRecord != null)
                        {
                            logRecord.Dispose();
                            logRecord = null;
                        }

                        if (logReader != null)
                        {
                            logReader.Dispose();
                            logReader = null;
                        }

                        if (xmlRecords.Any())
                        {
                            outputWriter.WriteLine("\t<EventLog LogName=\"{0}\">", logFile.Name.Replace(logFile.Extension, string.Empty).Replace("%4", "/"));
                            xmlRecords.ForEach(i =>
                            {
                                outputWriter.WriteLine("\t\t{0}", i);
                                outputWriter.Flush();
                            });

                            outputWriter.WriteLine("\t</EventLog>");
                            outputWriter.Flush();
                        }

                        xmlRecords.Clear();
                        xmlRecords = null;
                    }
                }

                outputWriter.WriteLine("</Logs>");
                outputWriter.Flush();

            }
            finally
            {
                if (outputStream != null)
                {
                    outputStream.Flush();
                    outputStream.Close();

                    outputStream.Dispose();
                    outputStream = null;
                }
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("This program has created a file called Events.xml, located within the same folder you ran this program from.");
            Console.WriteLine("You'll need to send me the xml file.");
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("Press any key to exit.");
            Console.Read();
        }
    }
}
