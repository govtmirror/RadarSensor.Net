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
        /// <param name="sweepParams"></param>
        /// <param name="sysMessage"></param>
        /// <returns>true if error</returns>
        public bool PerformCal(SweepParams sweepParams, SysMessage sysMessage)
        {
            List<double> powerListNdOn = new List<double>();
            List<double> powerListNdOff = new List<double>();

            double attenuaiton;

            if (sweepParams.Attenuation > MAX_ATTEN ||
                sweepParams.Attenuation < MIN_ATTEN)
            {
                Utilites.logMessage("Attenuation out of range for sensor");
                return true; ;
            }
            else
            {
                sweepParams.MinAtten = sweepParams.Attenuation;
                sweepParams.MaxAtten = sweepParams.Attenuation;
            }

            // set filter 
            bool err = SetFilter(sweepParams.sys2Detect);
            if (err)
            {
                return true;
            }

            // Load sysMessage with sweepParam values
            sysMessage.calibration.measurementParameters.detector =
                sweepParams.Detector;
            sysMessage.calibration.measurementParameters.window =
                sweepParams.Window;
            sysMessage.calibration.measurementParameters.attenuation =
                sweepParams.Attenuation;
            sysMessage.calibration.measurementParameters.videoBw = -1;
            sysMessage.calibration.temp = preselector.GetTemp();
            sysMessage.calibration.measurementParameters.dwellTime =
                sweepParams.DwellTime;

            // perfrom cal for number of attenuations
            // IS THIS necessary???
            //int iterations = 1 +
            //    (measParams.MaxAtten - measParams.Attenuation)
            //    / measParams.StepAtten;
            //for (int i = 0; i < iterations; i++)
            //{
            FFTParams fftParams = 
                new FFTParams(sensorCapabilities, sweepParams, 
                possibleSampleRates, possibleSpans);

            if (fftParams.Error)
            {
                Utilites.logMessage("error calculating FFT Params");
                return true;
            }

            // load sysMessage with fftParams and perfrom sweep for cal
            fftParams.LoadSysMessage(sysMessage);

            // Perform sweep with calibrated noise source on
            preselector.SetNdIn();
            preselector.PowerOnNd();
            DetectSpan(fftParams, powerListNdOn, sweepParams);

            // perfrom sweep with calibrated noise source off
            preselector.PowerOffNd();
            DetectSpan(fftParams, powerListNdOff, sweepParams);

            // Y-Factor Calibration
            Yfactor yFactorCal;
            if (powerListNdOff.Count != powerListNdOn.Count) // sanity check
            {
                Utilites.logMessage("Error getting sweep data.  " +
                 "Noise diode on and off power list are different sizes");
                return true;
            }

            yFactorCal = new Yfactor(powerListNdOn, powerListNdOff,
                    (double)sysMessage.calibration.measurementParameters.resolutionBw,
                    (double)sysMessage.calibration.measurementParameters.equivalentNoiseBw,
                    sweepParams.DwellTime,
                    sysMessage.preselector.excessNoiseRatio,
                    sysMessage.antenna.cableLoss,
                    sysMessage.antenna.gain);

            sysMessage.calibration.processed = "true";
            sysMessage.gain = yFactorCal.GainDbw;
            sysMessage.noiseFigure = yFactorCal.NoseFigureDbw;
            return false;
        }
        //}
        #endregion

        #region private methods 
        private void DetectSpan(FFTParams fftParams, 
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
              
                DetectSegment(measParams, fftParams, powerList,
                    cf, numFftsToCopy);
            }
        }

        private void DetectSegment(SweepParams measParams, 
            FFTParams fftParams, List<double> powerList,
            double cf, uint numFftsToCopy)
        {
            AgSalLib.SalError err;

            AgSalLib.SweepParms sweepParams = new AgSalLib.SweepParms();
            AgSalLib.FrequencySegment[] fs 
                = new AgSalLib.FrequencySegment[1];

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
            sweepParams.monitorInterval = 0.5f;

            fs[0].numFftPoints = fftParams.NumFftBins;
            fs[0].numAverages = fftParams.NumFftsToAvg;
            fs[0].firstPoint = 0;
            fs[0].numPoints = fs[0].numFftPoints;
            fs[0].centerFrequency = cf;
            fs[0].sampleRate = fftParams.SampleRate;
            fs[0].preamp = measParams.PreAmp;
            fs[0].attenuation = measParams.Attenuation;

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
            int maxDataReadMilliSeconds = 100;
            DateTime t0 = DateTime.Now;
            bool dataRetrieved = false;

            while (!dataRetrieved)
            {
                TimeSpan elapsedTime = DateTime.Now.Subtract(t0);
                if (elapsedTime.Milliseconds > maxDataReadMilliSeconds)
                {
                    Utilites.logMessage("Getting segment data timed out, " +
                        "restarting Calibration");
                    this.PerformCal(measParams, new SysMessage());
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
                            Utilites.logMessage(message);
                            // return an error 
                            break;
                        }
                        //get the data 
                        // cast frequencyData as doubles and add to powerLists 
                        FloatArrayToListOfDoubles(frequencyData, 
                        powerList, numFftsToCopy);
                        dataRetrieved = true;
                        break;

                    case AgSalLib.SalError.SAL_ERR_NO_DATA_AVAILABLE:
                        // data is not available yet ... 
                        break;
                    default:
                        // restart cal
                        SensorError(err, "salGetSegmentData");
                        break;
                }
            }
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
                Utilites.logMessage("invalid sys2Detect " +
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
                Utilites.logMessage(message);
                return true;
            }
            return false;
        }

        private void FloatArrayToListOfDoubles(float[] array, 
            List<double> list, uint sizeToCopy)
        {
            for (int i = 0; i < sizeToCopy; i++)
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
