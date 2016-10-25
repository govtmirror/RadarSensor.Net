using System;


namespace JsonClasses
{
    public class LocMessage : Message
    {
        public LocMessage() : base()
        {
      
        }

        public string mobility
        {
            get; set;
        }

        public string environmnet
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
    }
}
