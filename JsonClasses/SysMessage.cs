using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonClasses
{
    public class SysMessage : Message
    {
        public SysMessage()
        {
            cal = new Cal();
            antenna = new Antenna();
            preselector = new Preselector();
            cotsSensor = new COTSsensor();
            mPar = new Mpar();
        }

        // measurement parameters 
        public Mpar mPar;
        public Antenna antenna;
        public Preselector preselector;
        public COTSsensor cotsSensor;
        public Cal cal;

        // raw measured data with noise diode on
        public double[] wOn { get; set; }

        // raw measured data with noise diode off
        public double[] wOff { get; set; }

        // noise figure referenced to input of preselector
        public double fn { get; set; }

        // system gain referenced to input of preselector
        public double g { get; set; }


        public class Antenna
        {
            // data that describes antenna
            public Antenna() { }

            public string Model { get; set; }

            // low frequency of operational range Hz
            public double fLow { get; set; }

            // high frequency of operational range Hz
            public double fHigh { get; set; }

            // antenna gain dBi
            public double g { get; set; }

            // horizontal 3-db beamwidth (degrees)
            public double bwH { get; set; }

            // vertical 3-db beamwidth (degrees)
            public double hwV { get; set; }

            //  direction of main beam in azimuthal plane (degrees)
            public double AZ { get; set; }

            // direction of main beam in elevation plane
            public double EL { get; set; }

            // polarization
            public string Pol { get; set; }

            // Cress-polarization discrimination (dB)
            public double XSD { get; set; }

            // voltage standing wave ratio
            public double VSWR { get; set; }

            // cable loss
            public double lCable { get; set; }
        }

        // data that describes RF hardware components
        public class Preselector
        {
            public Preselector() { }

            // low frequency (Hz) of filter 1-db passband
            public double fLowPassBPF { get; set; }

            // high frequency (Hz) of filter 1-db passband
            public double fHighPassBPF { get; set; }

            // low frequency stop band (Hz)
            public double fLowStopBPF { get; set; }

            // high freqency stop band
            public double fHighStopBPF { get; set; }

            // noise figure of LNA (dB)
            public double fnLNA { get; set; }

            // gain of LNA 
            public double gLNA { get; set; }

            // max power at output of LNA
            public double pMaxLNA { get; set; }

            // excess noise ratio of noise diode for y-factor calibrations
            public double enrND { get; set; }
        }

        public class COTSsensor
        {
            public COTSsensor() { }

            public string Model { get; set; }

            // low frequency of operational range (Hz)
            public double fLow { get; set; }

            // high frequency of operational range (Hz)
            public double fHigh { get; set; }

            // noise figure 
            public double fn { get; set; }

           // maximum power (dBm)
           public double pMax { get; set; }
        }

        public class Cal
        {
            public Cal() { }

            public int CalsPerHour { get; set; }

            public double Temp { get; set; }

            public string mType { get; set; }

            // number of measurments per hour
            public int nM { get; set; }

            public string Processed { get; set; }

            public string DataTyple { get; set; }

            public string ByteOrder { get; set; }

            // compression of data 
            public string Compression { get; set; }
        }
    }
}
