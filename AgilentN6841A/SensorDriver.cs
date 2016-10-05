using System;
using System.IO;
using Newtonsoft.Json.Linq;
using AgSal;
using Logging;
using System.Collections.Generic;

namespace AgilentN6841A
{
    class SensorDriver
    {       
        // sensorHandle is used to talk to a specific sensor
        private IntPtr smsHandle = IntPtr.Zero; // handle to the Sensor Management Server
        private IntPtr sensorHandle = IntPtr.Zero; // handle to the sensor
        private IntPtr measHandle = IntPtr.Zero; // handle to the sweep measurement

        // measurement paramater data structures 
        private AgSalLib.SweepParms sweepParams;
        private AgSalLib.TunerParms tunerParams;

        // loads sweepParams and turnerParams from JSON file
        JObject measurementParams;

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

        public void SensorPerfromSweep()
        {
            // mean power table:  mean power (dbm) 
            List<double[]> meanPowerTable = new List<double[]>();
            List<double> attenList = new List<double>();

            sweepParams = new AgSalLib.SweepParms();
            tunerParams = new AgSalLib.TunerParms();

            int numOfFrequencies = measurementParams.Value<int>("n");
            // running loop small number of times while 
            for (int i = 0; i < 1; i++)
            {
                FFTParams fftParams = new FFTParams(sensorCapabilities)
                {   
                    Fstart = measurementParams.Value<double>("fStart"),
                    Fstop = measurementParams.Value<double>("fStop"),
                    Window = measurementParams.Value<string>("window"),
                    Detector = measurementParams.Value<string>("det"),
                    Bw = measurementParams.Value<double>("bw"),
                    TimeOverlap = measurementParams.Value<double>("timeOverlap"),
                    RmvAa = measurementParams.Value<int>("rmvAa"),
                    PreAmp = measurementParams.Value<double>("preAmp"),
                    Antenna = measurementParams.Value<double>("antenna"),
                    DwellTime = measurementParams.Value<double>("dwellTime"),
                    Attenuation = measurementParams.Value<double>("attenuation")
                };
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

        private void loadParamsFromJson()
        {
            // get mPar JSON file
            string file = @"C:\GitHub\RadarSensor\AgilentN6841A\mPar\sweep1.json";
            StreamReader stream = new StreamReader(file);
            string jSonString = stream.ReadToEnd();
            measurementParams = JObject.Parse(jSonString);
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
            sensor.loadParamsFromJson();
            sensor.SensorPerfromSweep();
            Console.Read();
        }
    }
}
