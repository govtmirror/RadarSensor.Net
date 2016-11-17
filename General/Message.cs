using System;
using System.Web.Script.Serialization;
using System.IO;

namespace General
{
    public class Message
    {
        public Message() { }

        public void loadMessageFields()
        {
            Config config =
               new JavaScriptSerializer().Deserialize<Config>(
                   File.ReadAllText(Constants.ConfigFile));
            sensorKey = config.SensorKey;
            sensorId = config.SensorHostName;
            version = Constants.TRANSFER_SPEC_VER;
            time = Utilites.GetEpochTime();
        }
        
        public string version
        {
            get; set;
        }

        public string messageType
        {
            get; set;
        }

        public string sensorId
        {
            get;
            set;
        }

        public int? sensorKey
        {
            get; set;
        }

        public long? time { get; set; }
    }
}
