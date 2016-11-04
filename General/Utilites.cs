using System;
using System.Text;
using System.IO;
using System.Web.Script.Serialization;

namespace General
{
    public enum MessageTypes
    {
        LocMessage,
        DataMessage,
        SysMessage
    };

    public static class Utilites
    {
        public static void WriteMessageToFile(Message messageClass)
        {
            string messageName = "";
            if (messageClass is LocMessage)
            {
                messageName = "LocMessage";
            }
            else if (messageClass is DataMessage)
            {
                messageName = "DataMessage";
            }
            else if (messageClass is SysMessage)
            {
                messageName = "SysMessage";
            }
            else
            {
                LogMessage("Invalid type of Message Object");
                return;
            }

            string messageJsonString =
                new JavaScriptSerializer().Serialize(messageClass);

            string fileName = messageName + "_" +
                DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".json";
            string path = Path.Combine(Constants.MESSAGE_FILES_DIR,
                fileName);
            System.IO.File.WriteAllText(path, messageJsonString);
        }

        public static long GetEpochTime()
        {
            return (long)DateTime.UtcNow.Subtract(new 
                DateTime(1970, 1, 1)).TotalMilliseconds;
            
        }

        public static void LogMessage(string msg)
        {
            Console.WriteLine("Logging message");
            StreamWriter w = File.AppendText(Constants.LogFile);
            w.WriteLine("---------- Log Entry ----------");
            w.WriteLine("{0} {1}", DateTime.Now.ToLongTimeString(),
                DateTime.Now.ToLongDateString());
            w.WriteLine(msg);
            w.WriteLine("----------------------------------------");
            w.WriteLine();
            w.Close();
        }

        public static void Main(String[] args) { }
    }
}
