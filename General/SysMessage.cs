using System;
using System.IO;
using System.Web.Script.Serialization;

namespace General
{
    public class SysMessage : Message
    {
        public SysMessage()
        {
            string antennaString =
                File.ReadAllText(Constants.AntennaFile);
            antenna = 
                new JavaScriptSerializer().Deserialize<Antenna>(antennaString);

            string preselectorString =
                File.ReadAllText(Constants.PreselectorFile);
            preselector = 
                new JavaScriptSerializer().Deserialize<Preselector>(preselectorString);

            string sensorString = 
                File.ReadAllText(Constants.CotsSensorFile);
            cotsSensor = 
                new JavaScriptSerializer().Deserialize<CotsSensor>(sensorString);

            string calString =
                File.ReadAllText(Constants.CalibrationFile);
            calibration =
                new JavaScriptSerializer().Deserialize<Calibration>(calString);
        }

        public Antenna antenna;
        public Preselector preselector;
        public CotsSensor cotsSensor;
        public Calibration calibration;

        // raw measured data with noise diode on
        public double[] noiseSourceOnPowers { get; set; }

        // raw measured data with noise diode off
        public double[] noiseSourceOffPowers { get; set; }

        // noise figure referenced to input of preselector
        public double[] noiseFigure { get; set; }

        // system gain referenced to input of preselector
        public double[] gain { get; set; }

        public class Antenna
        {
            // data that describes antenna
            public Antenna() { }

            public string model { get; set; }

            // low frequency of operational range Hz
            public double lowFrequency { get; set; }

            // high frequency of operational range Hz
            public double highFrequency { get; set; }

            // antenna gain dBi
            public double gain { get; set; }

            // horizontal 3-db beamwidth (degrees)
            public double horizontalBeamWidth { get; set; }

            // vertical 3-db beamwidth (degrees)
            public double verticalBeamWidth { get; set; }

            //  direction of main beam in azimuthal plane (degrees)
            public double azmithBeamDir { get; set; }

            // direction of main beam in elevation plane
            public double elevationBeamDir { get; set; }

            // polarization
            public string polarization { get; set; }

            // Cress-polarization discrimination (dB)
            public double crossPolarDiscrimination { get; set; }

            // voltage standing wave ratio
            public double voltageStandingWaveRatio { get; set; }

            public double cableLoss { get; set; }
        }

        // data that describes RF hardware components
        public class Preselector
        {
            public Preselector() { }

            // low frequency (Hz) of filter 1-db passband
            public double lowFreqPassband { get; set; }

            // high frequency (Hz) of filter 1-db passband
            public double highFreqPassband{ get; set; }

            // low frequency stop band (Hz)
            public double lowFreqStopband { get; set; }

            // high freqency stop band
            public double highFreqStopband { get; set; }

            // noise figure of LNA (dB)
            public double lnaNoiseFigure { get; set; }

            // gain of LNA 
            public double lnaGain { get; set; }

            // max power at output of LNA
            public double lnamaxPowerOut { get; set; }

            // excess noise ratio of noise diode for y-factor calibrations
            public double excessNoiseRatio { get; set; }
        }

        public class CotsSensor
        {
            public CotsSensor() { }

            public string model { get; set; }

            // low frequency of operational range (Hz)
            public double lowFrequency { get; set; }

            // high frequency of operational range (Hz)
            public double highFrequency { get; set; }

            // noise figure 
            public double noiseFigure { get; set; }

           // maximum power (dBm)
           public double maxPower { get; set; }
        }

        public class Calibration
        {
            public Calibration() 
            { 
                measurementParameters = new MeasurementParameters();
            }

            public MeasurementParameters measurementParameters;

            public int? calsPerHour { get; set; }

            public double? temp { get; set; }

            public string measurementType { get; set; }

            // number of measurments per calibration
            public int? numOfMeasurementsPerCal { get; set; }

            public bool processed { get; set; }

            public string dataType { get; set; }

            public string byteOrder { get; set; }

            // compression of data 
            public string compression { get; set; }
        }

        public override string ToString()
        {
            return "SysMessage";
        }
    }
}
