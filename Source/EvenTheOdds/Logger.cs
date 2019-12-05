using System;
using System.IO;

namespace EvenTheOdds
{
    public class Logger
    {
        static string filePath = $"{EvenTheOdds.ModDirectory}/EvenTheOdds.log";
        public static void LogError(Exception ex)
        {
            if (EvenTheOdds.DebugLevel >= 1)
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    var prefix = "[EvenTheOdds @ " + DateTime.Now.ToString() + "]";
                    writer.WriteLine("Message: " + ex.Message + "<br/>" + Environment.NewLine + "StackTrace: " + ex.StackTrace + "" + Environment.NewLine);
                    writer.WriteLine("----------------------------------------------------------------------------------------------------" + Environment.NewLine);
                }
            }
        }

        public static void LogLine(String line, bool separator = false)
        {
            if (EvenTheOdds.DebugLevel >= 2)
            {
                using (StreamWriter writer = new StreamWriter(filePath, true))
                {
                    var prefix = "[EvenTheOdds @ " + DateTime.Now.ToString() + "]";
                    writer.WriteLine(prefix + line);

                    if(separator)
                    {
                        writer.WriteLine("---");
                    }
                }
            }
        }
    }
}
