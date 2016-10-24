using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonClasses
{
    public class Mpar
    {
        public Mpar() { }

        // start ant stop freqency 
        public double fStart { get; set; }
        public double fStop { get; set; }

        // number of frequencies in measurment
        public int n { get; set; }
        //dwell time 
        public double td { get; set; }
        public string Det { get; set; }

        // resolution bandwidth (Hz)
        public double RBW { get; set; }
        public double VBW { get; set; }
        public string window { get; set; }
        public double ENBW { get; set; }
        public int Atten;
    }
}
