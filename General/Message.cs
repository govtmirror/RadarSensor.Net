using System;
using System.Web;
using General;

namespace General
{
    public class Message
    {
        public Message() { }

        public void loadMessageFields()
        {
            version = Constants.TRANSFER_SPEC_VER;
            sensorId = Constants.SENSOR_HOST_NAME;
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
