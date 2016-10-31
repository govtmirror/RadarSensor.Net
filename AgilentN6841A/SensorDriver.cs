using System;
using System.IO;
using JsonClasses;
using AgSal;
using General;
using Logging;
using System.Collections.Generic;
using System.Linq;
using SensorFrontEnd;
using System.Threading;

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
            calcSampleRates();
        }

        #region public methods
        /// <summary>
        /// 
        /// </summary>
        /// <param name="band"> frequency band for measurement
        /// </param>
        public void performCal(SweepParams measParams, SysMessage sysMessage)
        {
            List<double> powerListNdOn = new List<double>();
            List<double> powerListNdOff = new List<double>();

            double attenuaiton;

            if (measParams.Attenuation > MAX_ATTEN ||
                measParams.Attenuation < MIN_ATTEN)
            {
                Logger.logMessage("Attenuation out of range for sensor");
                return;
            }
            else
            {
                measParams.MinAtten = measParams.Attenuation;
                measParams.MaxAtten = measParams.Attenuation;
            }

            // set filter 
            if (measParams.sys2Detect.ToLower().Equals("spn43"))
            {
                preselector.set3_5Filter();
            }
            else if (measParams.sys2Detect.ToLower().Equals("boatnav"))
            {
                preselector.set3_0Filter();
            }
            else if (measParams.sys2Detect.ToLower().Equals("ASR"))
            {
                preselector.setBypass();
            }
            else
            {
                Logger.logMessage("invalid sys2Detect " +
                    "in Measurement parameters object");
                return;
            }

            // perfrom cal for number of attenuations
            // IS THIS necessary???
            //int iterations = 1 +
            //    (measParams.MaxAtten - measParams.Attenuation)
            //    / measParams.StepAtten;
            //for (int i = 0; i < iterations; i++)
            //{
            FFTParams fftParams = new FFTParams(sensorCapabilities,
                measParams, possibleSampleRates, possibleSpans);
            if (fftParams.Error)
            {
                // TODO figure out best was to handle error 
                // with FFT calculations
                Logger.logMessage("error calculating FFT Params");
                return;
            }

            // populate SysMessage values
            sysMessage.calibration.measurementParameters.resolutionBw =
                fftParams.SampleRate / fftParams.NumFftBins;
            sysMessage.calibration.measurementParameters.startFrequency =
                fftParams.FrequencyList[0];
            sysMessage.calibration.measurementParameters.stopFrequency =
                fftParams.FrequencyList[fftParams.FrequencyList.Count - 1];
            sysMessage.calibration.measurementParameters
                .numOfFrequenciesInSweep = fftParams.FrequencyList.Count;
            sysMessage.calibration.measurementParameters.detector =
                measParams.Detector;
            sysMessage.calibration.measurementParameters.window =
                measParams.Window;
            sysMessage.calibration.measurementParameters.attenuation =
                measParams.Attenuation;
            sysMessage.calibration.measurementParameters.videoBw = -1;
            sysMessage.calibration.measurementParameters.
                equivalentNoiseBw = fftParams.WindowValue *
                fftParams.SampleRate / fftParams.NumFftBins;
            sysMessage.calibration.temp = preselector.getTemp();
            // dwell time?

            preselector.setNdIn();
            // Perform sweep with calibrated noise source on
            preselector.powerOnNd();
            detectSpan(fftParams, powerListNdOn, measParams);

            // perfrom sweet with calibrated noise source off
            preselector.powerOffNd();
            detectSpan(fftParams, powerListNdOff, measParams);

            // Y-Factor Calibration
            Yfactor yFactorCal;
            if (powerListNdOff.Count == powerListNdOn.Count) // sanity check
            {
                yFactorCal = new Yfactor(powerListNdOn, powerListNdOff, 
                    sysMessage.preselector.excessNoiseRatio, 
                    (double)sysMessage.calibration.
                    measurementParameters.equivalentNoiseBw);
            }
            else
            {
                Logger.logMessage("Error getting sweep data.  " +
                    "Noise diode on and off power list are different sizes");
            }
        }
        //}
        #endregion

        #region private methods 
        private void detectSpan(FFTParams fftParams, 
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
              
                detectSegment(measParams, fftParams, powerList,
                    cf, numFftsToCopy);
            }
        }
        private void detectSegment(SweepParams measParams, 
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
                    Logger.logMessage("Getting segment data timed out, " +
                        "restarting Calibration");
                    this.performCal(measParams, new SysMessage());
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
                            Logger.logMessage(message);
                            // return an error 
                            break;
                        }
                        //get the data 
                        // cast frequencyData as doubles and add to powerLists 
                        floatArrayToListOfDoubles(frequencyData, 
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
                Logger.logMessage(message);
                return true;
            }
            return false;
        }

        private void floatArrayToListOfDoubles(float[] array, 
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
