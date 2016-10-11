using System;
using System.IO;
using JsonClasses;
using AgSal;
using Logging;
using System.Collections.Generic;
using System.Linq;
using SensorFrontEnd;

namespace AgilentN6841A
{
    public enum Mband
    {
        ASR = 0,  // 2.8 bypass 
        BoatNav = 1, // 3.0 no bypass 
        SPN43 = 1 // 3.5 bypass 
    }

    public class SensorDriver
    {
        // Agilent N6841A specefic
        public const int MAX_ATTEN = 30;
        public const int MIN_ATTEN = 0;

        // sensorHandle is used to talk to a specific sensor
        private IntPtr smsHandle = IntPtr.Zero; // handle to the Sensor Management Server
        private IntPtr sensorHandle = IntPtr.Zero; // handle to the sensor
        private IntPtr measHandle = IntPtr.Zero; // handle to the sweep measurement

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
            AgSalLib.salSensorBeep(sensorHandle);
            calcSampleRates();
        }

        #region public methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="band"> frequency band for measurement
        /// </param>
        public void SensorPerfromSweep(Mband band, Preselector preselector,
            ref List<float> powerList, ref List<float> attenList)
        {
            MeasurmentParams measParams;

            // TODO add json files for other bands and include in check
            if (band == Mband.SPN43)
            {
                string json = File.ReadAllText(@"C:\GitHub\RadarSensor\AgilentN6841A\mPar\3_5ghz.json");
                measParams = 
                    new System.Web.Script.Serialization.
                    JavaScriptSerializer().Deserialize< MeasurmentParams>(
                        json);
                preselector.setRfIn();
            }
            else //TODO
            {
                string json = File.ReadAllText(@"C:\GitHub\RadarSensor\AgilentN6841A\mPar\3_5ghz.json");
                measParams =
                    new System.Web.Script.Serialization.
                    JavaScriptSerializer().Deserialize<MeasurmentParams>(
                        json);
            }
            
            if (measParams.Attenuation != MAX_ATTEN)
            {
                measParams.MaxAtten = measParams.Attenuation;
            }

            int iterations = 1 +
                (measParams.MaxAtten - measParams.Attenuation)
                / measParams.StepAtten;
            for (int i = 0; i < iterations; i++)
            {
                FFTParams fftParams = new FFTParams(sensorCapabilities,
                    measParams, possibleSampleRates, possibleSpans);
                if (fftParams.Error)
                {
                    // TODO figure out best was to handle error 
                    // with FFT calculations
                }
                detectSpan(measParams, fftParams, ref powerList);
            }
        }
        #endregion

        #region prvate methods 
        private void detectSpan(MeasurmentParams measParams, 
            FFTParams fftParams, ref List<float> powerList)
        {
            AgSalLib.SalError err;

            // perfrom measurement on total number of segments
            for (int i = 0; i < fftParams.NumSegments; i++)
            {
                AgSalLib.SweepParms sweepParams = new AgSalLib.SweepParms();
                AgSalLib.FrequencySegment[] fs 
                    = new AgSalLib.FrequencySegment[1];

                double cf = fftParams.CenterFrequencies[i];
                if (cf < sensorCapabilities.minFrequency ||
                    cf > sensorCapabilities.maxFrequency)
                {
                    Logger.logMessage("center frequency is invalid: "
                        + "\n" + "index " + i + " " + cf);
                }

                switch(measParams.Window)
                {
                    case "Hanning": sweepParams.window =
                            AgSalLib.WindowType.Window_hann;
                        break;
                    case "Gauss-Top": sweepParams.window =
                            AgSalLib.WindowType.Window_gaussTop;
                        break;
                    case "Flattop": sweepParams.window =
                            AgSalLib.WindowType.Window_flatTop;
                        break;
                    case "Rectangular": sweepParams.window =
                            AgSalLib.WindowType.Window_uniform;
                        break;
                }
                switch (measParams.TimeOverlap)
                {
                    case 0: fs[0].overlapType = 
                            AgSalLib.OverlapType.OverlapType_on;
                        break;
                    default: fs[0].overlapType =
                            AgSalLib.OverlapType.OverlapType_off;
                        break;
                }
                switch (measParams.Detector)
                {
                    case "RMS": fs[0].averageType =
                            AgSalLib.AverageType.Average_rms;
                        break;
                    case "Sample": fs[0].averageType =
                            AgSalLib.AverageType.Average_off;
                        break;
                    case "Positive": fs[0].averageType =
                            AgSalLib.AverageType.Average_peak;
                        break;
                }
                switch (measParams.Antenna)
                {
                    case 0: fs[0].antenna = 
                            AgSalLib.AntennaType.Antenna_Terminated2;
                        break;
                    case 1: fs[0].antenna =
                            AgSalLib.AntennaType.Antenna_1;
                        break;
                    case 2: fs[0].antenna =
                            AgSalLib.AntennaType.Antenna_2;
                        break;
                }
                sweepParams.numSweeps = 1;
                sweepParams.numSegments = 1;
                sweepParams.monitorMode = AgSalLib.MonitorMode.MonitorMode_off;
                sweepParams.monitorInterval = 0.5;

                fs[0].numFftPoints = fftParams.NumFftBins;
                fs[0].numAverages = fftParams.NumFftsToAvg;
                fs[0].firstPoint = 0;
                fs[0].numPoints = fs[0].numFftPoints;
                fs[0].centerFrequency = cf;
                fs[0].sampleRate = fftParams.SampleRate;
                fs[0].preamp = measParams.PreAmp;
                fs[0].attenuation = measParams.Antenna;

                // Setup pacing
                AgSalLib.salFlowControl flowControl = new AgSalLib.salFlowControl();
                flowControl.pacingPolicy = 1; // wait when full 
                flowControl.maxBacklogMessages = 50;

                err = AgSalLib.salStartSweep2(out measHandle, sensorHandle,
                    ref sweepParams, ref fs, ref flowControl, null);
                if (SensorError(err, "salStartSweep"))
                {
                    return;
                }

                AgSalLib.SweepStatus status;
                double elapsed;
                AgSalLib.salGetSweepStatus2(measHandle, 0, out status, out elapsed);
                // busy waiting
                while (status == AgSalLib.SweepStatus.SweepStatus_running)
                {
                    AgSalLib.salGetSweepStatus2(measHandle, 0, out status, out elapsed);
                }

                // get data from sweep
                AgSalLib.SegmentData segmentData = new AgSalLib.SegmentData();
                float[] frequencyData = new float[fftParams.NumFftBins];

                err = AgSalLib.salGetSegmentData(measHandle, 
                    out segmentData, frequencyData, 
                    (uint)frequencyData.Length * 4);

                if (SensorError(err, "salGetSegmentData"))
                {
                    return;
                }
                if (segmentData.errorNum != AgSalLib.SalError.SAL_ERR_NONE)
                {
                    Logger.logMessage("Error in segment data header");
                }
                List<float> lst = frequencyData.OfType<float>().ToList();
                powerList.AddRange(lst);
            }
        }

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
            
        }
    }
}
