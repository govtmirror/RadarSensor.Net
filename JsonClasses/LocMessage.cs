using System;


namespace JsonClasses
{
    public class LocMessage : Message
    {
        public LocMessage() { }

        public string Mobility
        {
            get; set;
        }

        public double Lat
        {
            get; set;
        }

        public double Lon
        {
            get; set;
        }

        public int Alt
        {
            get; set;
        }

        public string TimeZone
        {
            get; set;
        }
    }
}
