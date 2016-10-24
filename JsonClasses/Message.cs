using System;
using System.Web;

namespace JsonClasses
{
    public class Message
    {
        public Message() { }
        
        public string Ver
        {
            get; set;
        }

        public string Type
        {
            get; set;
        }

        public string SensorID
        {
            get; set;
        }

        public long SensorKey
        {
            get; set;
        }

        public long t { get; set; }
    }
}
