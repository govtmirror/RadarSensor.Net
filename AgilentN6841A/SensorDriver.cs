using System;
using System.IO;
using AgSal;
using General;
using System.Collections.Generic;
using System.Linq;
using SensorFrontEnd;
using System.Threading;

namespace AgilentN6841A
{
    public class SensorDriver
    {
        Preselector preselector = 
            new Preselector(Constants.PRESELECTOR_IP);
        // Agilent N6841A specific
        public const int MAX_ATTEN = 30;
        public const int MIN_ATTEN = 0;

        // sensorHandle is used to talk to a specific sensor
        private IntPtr smsHandle = IntPtr.Zero; // handle to the Sensor Management Server
        private IntPtr sensorHandle = IntPtr.Zero; // handle to the sensor
        private IntPtr measHandle = IntPtr.Zero; // handle to the sweep measurement

        private AgSalLib.SensorCapabilities sensorCapabilities;

        private string smsHostName;
        private string sensorName;

        // possible sample rates for sensor
        private double[] possibleSampleRates;
        private double[] possibleSpans;

        #region Poperties
        public AgSalLib.SensorCapabilities SensorCapabilities
        {
            get { return sensorCapabilities; }
        }

        public double[] PossibleSampleRates
        {
            get { return possibleSampleRates; }
        }

        public double[] PossibleSpans
        {
            get { return possibleSpans; }
        }
        #endregion 
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ip">ip address in dotted decimal</param>
        public SensorDriver()
        {
            sensorName = Constants.SENSOR_HOST_NAME;

            bool connectionPassed = ConnectSensor();
            if (!connectionPassed)
            {
                Console.WriteLine("Connecting to sensor failed");
                Environment.Exit(0); 
            }
            AgSalLib.salSensorBeep(sensorHandle);
            CalcSampleRates();
        }

        #region public methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="calParams"></param>
        /// <param name="sysMessage"></param>
        /// <returns>true if error</returns>
        public void PerformCal(SweepParams calParams, 
            SysMessage sysMessage, out YfactorCal yFactorCal)
        {
            List<double> powerListNdOn = new List<double>();
            List<double> powerListNdOff = new List<double>();

            double attenuaiton;

            if (calParams.Attenuation > MAX_ATTEN ||
                calParams.Attenuation < MIN_ATTEN)
            {
                Utilites.LogMessage("Attenuation out of range for sensor");
                yFactorCal = null;
                return;
            }
            else
            {
                calParams.MinAtten = calParams.Attenuation;
                calParams.MaxAtten = calParams.Attenuation;
            }

            // set filter 
            bool err = SetFilter(calParams.sys2Detect);
            if (err)
            {
                yFactorCal = null;
                return;
            }

            // Load sysMessage with sweepParam values
            sysMessage.calibration.measurementParameters.detector =
                calParams.Detector;
            sysMessage.calibration.measurementParameters.window =
                calParams.Window;
            sysMessage.calibration.measurementParameters.attenuation =
                calParams.Attenuation;
            sysMessage.calibration.measurementParameters.videoBw = -1;
            sysMessage.calibration.temp = preselector.GetTemp();
            sysMessage.calibration.measurementParameters.dwellTime =
                calParams.DwellTime;

            // perfrom cal for number of attenuations
            // IS THIS necessary???
            //int iterations = 1 +
            //    (measParams.MaxAtten - measParams.Attenuation)
            //    / measParams.StepAtten;
            //for (int i = 0; i < iterations; i++)
            //{
            FFTParams fftParams = 
                new FFTParams(sensorCapabilities, calParams, 
                possibleSampleRates, possibleSpans);

            if (fftParams.Error)
            {
                Utilites.LogMessage("error calculating FFT Params");
                yFactorCal = null;
                return;
            }

            // load sysMessage with fftParams and perfrom sweep for cal
            fftParams.LoadSysMessage(sysMessage);

            // Perform sweep with calibrated noise source on
            preselector.SetNdIn();
            preselector.PowerOnNd();
            err = DetectSpan(fftParams, powerListNdOn, calParams);
            if (err)
            {
                yFactorCal = null;
                return;
            }

            // perfrom sweep with calibrated noise source off
            preselector.PowerOffNd();
            err = DetectSpan(fftParams, powerListNdOff, calParams);
            if (err)
            {
                yFactorCal = null;
                return;
            }

            // Y-Factor Calibration
            if (powerListNdOff.Count != powerListNdOn.Count) // sanity check
            {
                Utilites.LogMessage("Error getting sweep data.  " +
                 "Noise diode on and off power list are different sizes");
                yFactorCal = null;
                return;
            }

            yFactorCal = new YfactorCal(powerListNdOn, powerListNdOff,
                    (double)sysMessage.calibration.measurementParameters.resolutionBw,
                    (double)sysMessage.calibration.measurementParameters.equivalentNoiseBw,
                    calParams.DwellTime,
                    sysMessage.preselector.excessNoiseRatio,
                    sysMessage.antenna.cableLoss,
                    sysMessage.antenna.gain);

            sysMessage.calibration.processed = true;
            sysMessage.gain = yFactorCal.GainDbw;
            sysMessage.noiseFigure = yFactorCal.NoseFigureDbw;
        }
        //}

        /// <summary>
        /// Performs a multi-segment detection with a Keysight N6841A sensor 
        /// and applies variable attenuation when overload occurs.
        /// </summary>
        /// <param name="sweepParams"></param>
        /// <param name="dataMessage"></param>
        public bool performMeasurement(SweepParams sweepParams, 
            DataMessage dataMessage, YfactorCal yFactorCal)
        {
            FFTParams fftParams = new FFTParams(sensorCapabilities,
                sweepParams, possibleSampleRates, possibleSpans);

            if (fftParams.Error)
            {
                Utilites.LogMessage("error calculating FFT Params");
                return true;
            }
            // set filter 
            bool err = SetFilter(sweepParams.sys2Detect);

            return false;
        }
        #endregion

        #region private methods 
        private bool DetectSpan(FFTParams fftParams, 
            List<double> powerList, SweepParams measParams)
        {
            // detect over span
            for (int j = 0; j < fftParams.NumSegments; j++)
            {
                double cf = fftParams.CenterFrequencies[j];

                uint numFftsToCopy;
                if (j == fftParams.NumFullSegments)
                {
                    numFftsToCopy = fftParams.NumBinsLastSegment;
                }
                else
                {
                    numFftsToCopy = fftParams.NumValidFftBins;
                }
              
                bool err = DetectSegment(measParams, fftParams, powerList,
                    cf, numFftsToCopy);
                if (err)
                {
                    return true;
                }
            }
            return false;
        }

        private bool DetectSegment(SweepParams measParams, 
            FFTParams fftParams, List<double> powerList,
            double cf, uint numFftsToCopy)
        {
            AgSalLib.SalError err;

            AgSalLib.SweepParms sweepParams = new AgSalLib.SweepParms();
            AgSalLib.FrequencySegment[] fs 
                = new AgSalLib.FrequencySegment[1];

            switch(measParams.Window.ToLower())
            {
                case "hanning": sweepParams.window =
                       sweepParams.window = AgSalLib.WindowType.Window_hann;
                    break;
                case "gauss-top": sweepParams.window =
                        sweepParams.window = AgSalLib.WindowType.Window_gaussTop;
                    break;
                case "flattop": sweepParams.window =
                        sweepParams.window = AgSalLib.WindowType.Window_flatTop;
                    break;
                case "rectangular": sweepParams.window =
                        sweepParams.window = AgSalLib.WindowType.Window_uniform;
                    break;
                default:
                    Utilites.LogMessage("Invalid window type in " +
                        "calibration json file");
                    return true;
            }

            switch (measParams.TimeOverlap)
            {
                case 50:
                    fs[0].overlapType = 
                        AgSalLib.OverlapType.OverlapType_on;
                    break;
                default:
                    fs[0].overlapType =
                        AgSalLib.OverlapType.OverlapType_off;
                    break;
            }

            switch (measParams.Detector.ToLower())
            {
                case "rms": fs[0].averageType =
                        fs[0].averageType = AgSalLib.AverageType.Average_rms;
                    break;
                case "sample": fs[0].averageType =
                        fs[0].averageType = AgSalLib.AverageType.Average_off;
                    break;
                case "positive": fs[0].averageType =
                        fs[0].averageType = AgSalLib.AverageType.Average_peak;
                    break;
                default:
                    Utilites.LogMessage("Invalid Detector type in " +
                        "calibration json file");
                    return true;
            }

            switch (measParams.Antenna)
            {
                case 0:
                    fs[0].antenna = 
                        AgSalLib.AntennaType.Antenna_Terminated2;
                    break;
                case 1:
                    fs[0].antenna =
                        AgSalLib.AntennaType.Antenna_1;
                    break;
                case 2:
                    fs[0].antenna =
                        AgSalLib.AntennaType.Antenna_2;
                    break;
                default:
                    Utilites.LogMessage("Invalid antenna value " +
                        "in cal json file");
                    return true;
            }

            sweepParams.numSweeps = 1;
            sweepParams.numSegments = 1;
            sweepParams.sweepInterval = 0.0;
            sweepParams.monitorMode = AgSalLib.MonitorMode.MonitorMode_off;
            sweepParams.monitorInterval = 0.0;
            // data return type for sweepParams is always real float32 dbm

            fs[0].numFftPoints = fftParams.NumFftBins;
            fs[0].numAverages = fftParams.NumFftsToAvg;
            fs[0].firstPoint = 0;
            fs[0].numPoints = fs[0].numFftPoints;
            fs[0].centerFrequency = cf;
            fs[0].sampleRate = fftParams.SampleRate;
            fs[0].preamp = measParams.PreAmp;
            fs[0].attenuation = measParams.Attenuation;
            fs[0].dataType = AgSalLib.FftDataType.FftData_db;

            // Setup pacing
            AgSalLib.salFlowControl flowControl = new AgSalLib.salFlowControl();
            flowControl.pacingPolicy = 1; // wait when full 
            flowControl.maxBacklogMessages = 50;

            err = AgSalLib.salStartSweep2(out measHandle, sensorHandle,
                ref sweepParams, ref fs, ref flowControl, null);
            if (SensorError(err, "salStartSweep"))
            {
                return true;
            }

            AgSalLib.SweepStatus status;
            double elapsed;
            AgSalLib.salGetSweepStatus2(measHandle, 0, out status, out elapsed);
            // wait until sweep is finished
            while (status == AgSalLib.SweepStatus.SweepStatus_running)
            {
                AgSalLib.salGetSweepStatus2(measHandle, 0, out status, out elapsed);
            }

            // get data from sweep
            AgSalLib.SegmentData dataHeader = new AgSalLib.SegmentData();
            // Agilent DLL requires float array even though it uses double 
            // everywhere else ..... annoying 
            float[] frequencyData = new float[fftParams.NumFftBins];

            // how long to read before exiting
            int maxDataReadMilliSeconds = 1000;
            DateTime t0 = DateTime.Now;
            bool dataRetrieved = false;

            while (!dataRetrieved)
            {
                TimeSpan elapsedTime = DateTime.Now.Subtract(t0);
                if (elapsedTime.Milliseconds > maxDataReadMilliSeconds)
                {
                    Utilites.LogMessage("Getting segment data timed out, " +
                        "restarting cal");
                    //PerformCal(measParams, new SysMessage(), out yFactorCal);
                }

                err = AgSalLib.salGetSegmentData(measHandle, 
                    out dataHeader, frequencyData, 
                    (uint)frequencyData.Length * 4);

                switch(err) 
                {
                    case AgSalLib.SalError.SAL_ERR_NONE:
                        if (dataHeader.errorNum != AgSalLib.SalError.SAL_ERR_NONE) 
                        {
                            string message = "Segment data header returned an error: \n\n";
                                message += "errorNumber: " + dataHeader.errorNum.ToString() + "\n";
                                message += "errorInfo:   " + dataHeader.errorInfo;
                            Utilites.LogMessage(message);
                            return true;
                        }
                        //get the data 
                        // check if need to remove anti aliasing 
                        int startIndex = 0;
                        if (measParams.RmvAa == 1)
                        {
                            startIndex = getStartIndex(fftParams.SampleRate,
                                (int)fftParams.NumFftBins);
                        }

                        FloatArrayToListOfDoubles(frequencyData, 
                        powerList, numFftsToCopy, startIndex);
                        dataRetrieved = true;
                        break;

                        // TODO:  calculate frequencies 

                    case AgSalLib.SalError.SAL_ERR_NO_DATA_AVAILABLE:
                        // data is not available yet ... 
                        break;
                    default:
                        // restart cal
                        SensorError(err, "salGetSegmentData");
                        break;
                }
            }
            return false;
        }

        private bool SetFilter(string sys2Detect)
        {
            if (sys2Detect.ToLower().Equals("spn43"))
            {
                preselector.Set3_5Filter();
            }
            else if (sys2Detect.ToLower().Equals("boatnav"))
            {
                preselector.Set3_0Filter();
            }
            else if (sys2Detect.ToLower().Equals("ASR"))
            {
                preselector.SetBypass();
            }
            else
            {
                Utilites.LogMessage("invalid sys2Detect " +
                    "in Measurement parameters object");
                return true;
            }
            return false;
        }

        // Calculates sample rates for Agilent sensor 
        // just need to to calculate once and use in FFTParams 
        private void CalcSampleRates()
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

            err = AgSalLib.salConnectSensor3(out sensorHandle, smsHandle, sensorName, 0, "Radar Sensor", 0);
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
                Utilites.LogMessage(message);
                return true;
            }
            return false;
        }

        // determines start index when copying raw data from sensor
        private int getStartIndex(double sampleRate, int numBinsInFft)
        {
            int numValidBins = (int)FFTParams.floorEven(numBinsInFft / 
                (sensorCapabilities.maxSampleRate / sensorCapabilities.maxSpan));
            int startIndex = (numBinsInFft - numValidBins) / 2 + 1;
            return startIndex;
        }

        private void FloatArrayToListOfDoubles(float[] array, 
            List<double> list, uint sizeToCopy, int startIndex)
        {
            for (int i = startIndex; i < sizeToCopy + startIndex; i++)
            {
                list.Add((double)array[i]);
            }
        }
        #endregion

        public static void Main(String[] args)
        {
            
        }
    }
}
