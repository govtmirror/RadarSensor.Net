using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonClasses
{
    public class DataMessage : Message
    {
        public string sysToDetect { get; set; }

        public string sensitivity { get; set; }

        public string measurementType { get; set; }

        // Time of 1st acquisition in a sequence 
        // seconds since Jan 1, 1970 UTC
        public long timeOfAcquisition { get; set; }

        // index of current acquisition in a sequence
        public int aquisitionIndex { get; set; }

        public int numOfMeasurements { get; set; }

        // imposed time between acquisition starts 
        public double timeBetweenAcquisitions { get; set; }

        public double timeBetweenStreams { get; set; }

        // overload flag
        public int overloadFlag { get; set; }

        // detected system noise power dBm
        public double[] detectedSysNoisePowers { get; set; }

        public string comment { get; set; }

        public string processed { get; set; }

        public string dataType { get; set; }

        public string byteOrder { get; set; }

        public string compression { get; set; }

        public MeasurementParameters measurementParameters { get; set; }

        /// <summary>
        /// if processed = false 
        /// dBm ref to input of COTS sensor
        /// </summary>
        public double[] rawMeasuredPowers { get; set; }

        /// <summary>
        /// if processed = true
        /// measured pwer vector [dbm ref to output of isotropic antenna]
        /// </summary>
        public double[] measuredPowers { get; set; }
    }
}
