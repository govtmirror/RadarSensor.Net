using System;
using System.Web;
using General;

namespace JsonClasses
{
    public class Message
    {
        public Message() { }
        
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

        public long sensorKey
        {
            get; set;
        }

        public long? time { get; set; }
    }
}
