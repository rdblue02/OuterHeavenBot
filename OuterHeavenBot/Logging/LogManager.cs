using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OuterHeavenBot.Logging
{
    class LogManager
    {
        static string logPath;
        static LogManager()
        {
            try
            {
                logPath = $"{Directory.GetCurrentDirectory()}\\bot_logs\\logs.txt";
                if (!File.Exists(logPath))
                {
                    File.AppendAllTextAsync(logPath,"");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e);
            }
        }
        Logger<T> GetLogger<T>() where T : class
        {
            return new Logger<T>(logPath);
        }
    }
}
