using System;
using System.IO;

namespace Logging
{
    public static class Logger
    {
        private const string logFilePath =
            @"C:\RadarSensor\radarSensorLog.txt";

        public static void logMessage(string msg)
        {
            Console.WriteLine("Logging message");
            StreamWriter w = File.AppendText(logFilePath);
            w.WriteLine("---------- Log Entry ----------");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine(msg);
            w.WriteLine("----------------------------------------");
            w.WriteLine();
            w.Close();
        }

        public static void Main(String[] args)
        {

        }
    }
}

