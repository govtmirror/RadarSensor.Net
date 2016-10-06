using System;
using System.IO;
using Newtonsoft.Json.Linq;
using AgSal;
using Logging;
using System.Collections.Generic;

namespace AgilentN6841A
{
    public enum Mband
    {
        ASR = 0,  // 2.8 bypass 
        BoatNav = 1, // 3.0 no bypass 
        SPN3 = 1 // 3.5 bypass 
    }

    class SensorDriver
    {       
        // sensorHandle is used to talk to a specific sensor
        private IntPtr smsHandle = IntPtr.Zero; // handle to the Sensor Management Server
        private IntPtr sensorHandle = IntPtr.Zero; // handle to the sensor
        private IntPtr measHandle = IntPtr.Zero; // handle to the sweep measurement

        // measurement paramater data structures 
        private AgSalLib.SweepParms sweepParams;
        private AgSalLib.TunerParms tunerParams;

        private AgSalLib.SensorCapabilities sensorCapabilities;

        private string smsHostName;

        // possile sample rates for sensor
        private double[] possibleSampleRates;
        private double[] possibleSpans;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip">ip address in dotted decimal</param>
        public SensorDriver(String ip)
        {
            bool connectionPassed = ConnectSensor();
            if (!connectionPassed)
            {
                Console.WriteLine("Connecting to sensor failed");
                Environment.Exit(0); 
            }
            //AgSalLib.salSensorBeep(sensorHandle);
            calcSampleRates();
            Console.WriteLine("sample rate to span ratio: " + 
                sensorCapabilities.sampleRateToSpanRatio);
        }

        #region public methods
        public void initSensorParams()
        {
            sweepParams = new AgSalLib.SweepParms();
            tunerParams = new AgSalLib.TunerParms();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="band"> frequency band for measurement
        /// </param>
        public void SensorPerfromSweep(Mband band)
        {
            MeasurmentParams measParams;
            // TODO add json files for other bands and include in check
            if (band == Mband.SPN3)
            {
                measParams = new MeasurmentParams(@"C:\GitHub\RadarSensor\AgilentN6841A\mPar\3_5ghz.json");
            }
            else
            {
                measParams = new MeasurmentParams(@"C:\GitHub\RadarSensor\AgilentN6841A\mPar\3_5ghz.json");
            }
            // mean power table:  mean power (dbm) 
            List<double[]> meanPowerTable = new List<double[]>();
            List<double> attenList = new List<double>();

            sweepParams = new AgSalLib.SweepParms();
            tunerParams = new AgSalLib.TunerParms();

            for (int i = 0; i < 1; i++)
            {
                FFTParams fftParams = new FFTParams(sensorCapabilities,
                    measParams);
                fftParams.calcFftParameters(possibleSampleRates, 
                    possibleSpans);       
            }
        }
        #endregion

        #region prvate methods 
        // Calculates sample rates for Agilent sensor 
        // just need to to calculate once and use in FFTParams 
        private void calcSampleRates()
        {
            // Sample Rate = 1.28 * Span : 1.28 given by sensor capabilities
            int maxDecimations = sensorCapabilities.maxDecimations;
            possibleSampleRates = new double[maxDecimations + 1];
            possibleSpans = new double[maxDecimations + 1];

            possibleSampleRates[0] = sensorCapabilities.maxSampleRate;
            possibleSpans[0] = sensorCapabilities.maxSpan;

            for (int i = 1; i < maxDecimations + 1; i++)
            {
                possibleSampleRates[i] = (possibleSampleRates[i - 1] / 2);
                possibleSpans[i] = possibleSampleRates[i] /
                        sensorCapabilities.sampleRateToSpanRatio;        
            }
            Console.WriteLine("possible sample rates: " + 
                string.Join(", ", possibleSampleRates) + "\n");
            Console.WriteLine("possible spans: " + 
                string.Join(", ", possibleSpans ) + "\n");
        }

        private bool ConnectSensor()
        {
            AgSalLib.SalError err;

            err = AgSalLib.salOpenSms(out smsHandle, smsHostName, 0, null);
            if (SensorError(err, "salOpenSms")) return false;

            err = AgSalLib.salConnectSensor3(out sensorHandle, smsHandle, "RM3420B", 0, "Radar Sensor", 0);
            if (SensorError(err, "salConnectSensor3")) return false;

            err = AgSalLib.salGetSensorCapabilities(sensorHandle, out sensorCapabilities);
            if (SensorError(err, "salGetSensorCapabilities")) return false;
            return true;
        }

        private bool SensorError(AgSalLib.SalError err, string functionName)
        {
            string message = "";
            if (err != AgSalLib.SalError.SAL_ERR_NONE)
            {
                message = functionName + " returned error " + err +
                    " (" + AgSalLib.salGetErrorString(err, AgSalLib.Localization.English) + ")\n";
                Logger.logMessage(message);
                return true;
            }
            return false;
        }
        #endregion

        public static void Main(String[] args)
        {
            SensorDriver sensor = new SensorDriver("10.6.6.14");
            sensor.SensorPerfromSweep(Mband.SPN3);
            Console.Read();
        }
    }

    /// <summary>
    /// Loads measurment paramaters from JSON file 
    /// </summary>
    public class MeasurmentParams
    {
        JObject measurementParams;
        // start frequency (Hz)
        private double fStart;
        // stop frequency (Hz)
        private double fStop;
        // percentage of time-domain samples to overlap in adjacent FFTs {0,50}
        private double timeOverlap;
        // lag indicating portion of FFT bins to return {0: all, 1: exclude bins affected by anti-aliasing filter}
        private int rmvAa;
        // bandwidth (Hz)
        private double bw;
        private double antenna;
        private double preAmp;
        private double dwellTime;
        private double attenuation;
        private string window;
        private string detector;
        private int numberOfFrequencies;

        public MeasurmentParams(string path)
        {
            // get mPar JSON file
            StreamReader stream = new StreamReader(path);
            string jSonString = stream.ReadToEnd();
            measurementParams = JObject.Parse(jSonString);

            fStart = measurementParams.Value<double>("fStart");
            fStop = measurementParams.Value<double>("fStop");
            window = measurementParams.Value<string>("window");
            detector = measurementParams.Value<string>("det");
            bw = measurementParams.Value<double>("bw");
            timeOverlap = measurementParams.Value<double>("timeOverlap");
            rmvAa = measurementParams.Value<int>("rmvAa");
            preAmp = measurementParams.Value<double>("preAmp");
            antenna = measurementParams.Value<double>("antenna");
            dwellTime = measurementParams.Value<double>("dwellTime");
            attenuation = measurementParams.Value<double>("attenuation");
            numberOfFrequencies = measurementParams.Value<int>("n");
        }

        public double Fstart
        {
            get { return fStart; }
        }

        public double Fstop
        {
            get { return fStop; }
        }

        public string Detector
        {
            get { return detector; }
            set { detector = value; }
        }

        public string Window
        {
            get { return window; }
        }

        public double TimeOverlap
        {
            get { return timeOverlap; }
            set { timeOverlap = value; }
        }

        public int RmvAa
        {
            get { return rmvAa; }
            set { rmvAa = value; }
        }

        public double Attenuation
        {
            get { return attenuation; }
            set { attenuation = value; }
        }
        public double Antenna
        {
            get { return antenna; }
            set { antenna = value; }
        }
        public double Bw
        {
            get { return bw; }
            set { bw = value; }
        }
        public double PreAmp
        {
            get { return preAmp; }
            set { preAmp = value; }
        }

        public double DwellTime
        {
            get { return dwellTime; }
            set { dwellTime = value; }
        }

        public int NumFrequencies
        {
            get { return numberOfFrequencies; }
        }
    }
}
