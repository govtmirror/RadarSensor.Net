using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonClasses
{
    public class DataMessage : Message
    {
        public string Sys2Detect { get; set; }

        public string Sensitivity { get; set; }

        public string mType { get; set; }

        // Time of 1st acquisition in a sequence 
        // seconds since Jan 1, 1970 UTC
        public long t1 { get; set; }

        // index of current acquisition in a sequence
        public int a { get; set; }

        public int nM { get; set; }

        // imposed time between acquisition starts 
        public double Ta { get; set; }

        // overload flag
        public int OL { get; set; }

        // detected system noise power dBm
        public double[] wnI { get; set; }

        public string Comment { get; set; }

        public string Processed { get; set; }

        public string DataType { get; set; }

        public string ByteOrder { get; set; }

        public string Compression { get; set; }

        public Mpar mPar { get; set; }

        // Raw meadured data array dBm
        public double[] w { get; set; }

        public Mpar mpar;
    }
}
