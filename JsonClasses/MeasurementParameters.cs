using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JsonClasses
{
    public class MeasurementParameters
    {
        public MeasurementParameters() { }

        // start ant stop freqency 
        public double? startFrequency { get; set; }
        public double? stopFrequency { get; set; }

        // number of frequencies in measurment
        public int? numOfFrequenciesInSweep { get; set; } 
        public double? dwellTime { get; set; }
        public string detector { get; set; }

        public double? resolutionBw { get; set; }
        public double? videoBw { get; set; }
        public string window { get; set; }
        public double? equivalentNoiseBw { get; set; }
        public int? attenuation { get; set; }
        public double? sampleRate { get; set; }
        public double[] centerFrequencies { get; set; }

    }
}
