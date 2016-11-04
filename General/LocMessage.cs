using System;


namespace General
{
    public class LocMessage : Message
    {
        public LocMessage()
        {
        }

        public string mobility
        {
            get; set;
        }

        public string environment
        {
            get; set;
        }

        public double latitude
        {
            get; set;
        }

        public double longitude
        {
            get; set;
        }

        public int altitude
        {
            get; set;
        }

        public string timeZone
        {
            get; set;
        }

        public override string ToString()
        {
            return "LocMessage";
        }
    }
}
