﻿namespace JsonClasses
{
    public class SweepParams
    {
        // start frequency (Hz)
        private double fStart;
        // stop frequency (Hz)
        private double fStop;
        private double bw; // bandwidth (hz)
        private double enbw;
        private double dwellTime; // (seconds)
        // percentage of time-domain samples to overlap in adjacent FFTs {0,50}
        private int timeOverlap;
        // lag indicating portion of FFT bins to return 
        //{0: all, 1: exclude bins affected by anti-aliasing filter}
        private int rmvAa;
        private int antenna;
        private int preAmp;
        private int attenuation;
        private int minAtten;
        private int maxAtten;
        private int stepAtten;

        private string window;
        private string detector;

        // parameterless constructor of object deserialization
        public SweepParams() { }
        #region Properties
        public double Fstart
        {
            get { return fStart; }
            set { fStart = value; }
        }

        public double Fstop
        {
            get { return fStop; }
            set { fStop = value; }
        }

        public string Detector
        {
            get { return detector; }
            set { detector = value; }
        }

        public string Window
        {
            get { return window; }
            set { window = value; }
        }

        public int TimeOverlap
        {
            get { return timeOverlap; }
            set { timeOverlap = value; }
        }

        public int RmvAa
        {
            get { return rmvAa; }
            set { rmvAa = value; }
        }

        public int Attenuation
        {
            get { return attenuation; }
            set { attenuation = value; }
        }
        public int Antenna
        {
            get { return antenna; }
            set { antenna = value; }
        }
        public double Bw
        {
            get { return bw; }
            set { bw = value; }
        }
        public int PreAmp
        {
            get { return preAmp; }
            set { preAmp = value; }
        }

        public double DwellTime
        {
            get { return dwellTime; }
            set { dwellTime = value; }
        }

        public int MinAtten
        {
            get { return minAtten; }
            set
            {
                minAtten = value;
            }
        }

        public int MaxAtten
        {
            get { return maxAtten; }
            set
            {
                maxAtten = value;
            }
        }

        public int StepAtten
        {
            get { return stepAtten; }
            set { stepAtten = value; }
        }

        public double Enbw
        {
            get;
            set;
        }

        public string sys2Detect { get; set; }
        #endregion

        public static void Main(string[] args) { }
    }
}