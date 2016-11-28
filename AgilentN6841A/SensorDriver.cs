using System;
using System.IO;
using AgSal;
using General;
using System.Collections.Generic;
using System.Linq;
using SensorFrontEnd;
using System.Web.Script.Serialization;

namespace AgilentN6841A
{
    public class SensorDriver
    {
        Preselector preselector; 
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
        public SensorDriver(string preselectorIp,
            string sensorName)
        {
            this.sensorName = sensorName;
            preselector = new Preselector(preselectorIp);

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
        /// Perfrom Y-factor calibration 
        /// </summary>
        /// <param name="calParams"></param>
        /// <param name="sysMessage"></param>
        /// <param name="yFactorCal"></param>
        public void PerformCal(SweepParams calParams, 
            SysMessage sysMessage, out YfactorCal yFactorCal)
        {
            List<double> powerListNdOn = new List<double>();
            List<double> powerListNdOff = new List<double>();

            calParams.MinAtten = calParams.Attenuation;
            calParams.MaxAtten = calParams.Attenuation;

            // set filter 
            bool err = SetFilter(calParams.sys2Detect);
            if (err)
            {
                yFactorCal = null;
                return;
            }

            sysMessage.calibration.temp = preselector.GetTemp();

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
            fftParams.LoadMessage(sysMessage);

            // Perform sweep with calibrated noise source on
            preselector.SetNdIn();
            preselector.PowerOnNd();
            err = DetectSpan(fftParams, calParams, powerListNdOn,
                null, null);
            if (err)
            {
                yFactorCal = null;
                return;
            }

            // perfrom sweep with calibrated noise source off
            preselector.PowerOffNd();
            err = DetectSpan(fftParams, calParams, powerListNdOff,
                null, null);
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
        public bool PerformMeasurement(SweepParams sweepParams, 
            DataMessage dataMessage, YfactorCal yFactorCal)
        {
            SetFilter(sweepParams.sys2Detect);
            preselector.SetRfIn();
            preselector.PowerOffNd();

            FFTParams fftParams = new FFTParams(sensorCapabilities,
                sweepParams, possibleSampleRates, possibleSpans);

            if (fftParams.Error)
            {
                Utilites.LogMessage("error calculating FFT Params");
                return true;
            }

            fftParams.LoadMessage(dataMessage);

            // set filter 
            bool err = SetFilter(sweepParams.sys2Detect);

            List<double> powerList = new List<double>();
            List<double> frequencyList = new List<double>();
            List<int> attenList = Enumerable.Repeat(sweepParams.Attenuation,
                (int)fftParams.NumSegments).ToList(); 

            // detect over span 
            for (int i = 0; i < fftParams.NumSegments; i++)
            {
                double cf = fftParams.CenterFrequencies[i];

                uint numFftsToCopy;
                if (i == fftParams.NumFullSegments)
                {
                    numFftsToCopy = fftParams.NumBinsLastSegment;
                }
                else
                {
                    numFftsToCopy = fftParams.NumValidFftBins;
                }

                if (sweepParams.DynamicAttenuation)
                {
                    bool overload = true;
                    while (overload && attenList[i] <= MAX_ATTEN)
                    {
                        DetectSegment(sweepParams, fftParams, powerList,
                            frequencyList, cf, numFftsToCopy, ref overload);
                        if (overload)
                        {
                            dataMessage.overloadFlag = true;
                            sweepParams.Attenuation += sweepParams.StepAtten;
                            attenList[i] = sweepParams.Attenuation;
                        }

                        // remove previous duplicated segment
                        int indexDuplicate = 0;
                        foreach (double number in frequencyList)
                        {
                            if (number == frequencyList[frequencyList.Count - 1])
                            {
                                indexDuplicate = frequencyList.IndexOf(number);
                                if (indexDuplicate == frequencyList.Count - 1)
                                {
                                    indexDuplicate = 0;
                                }
                                break;
                            }
                        }

                        if (indexDuplicate != 0)
                        {
                            frequencyList.RemoveRange(indexDuplicate - Convert.ToInt32(numFftsToCopy) + 1, Convert.ToInt32(numFftsToCopy));
                            powerList.RemoveRange(indexDuplicate - Convert.ToInt32(numFftsToCopy) + 1, Convert.ToInt32(numFftsToCopy));
                        }

                        // end removing

                    }
                }
                else
                {
                    bool overload = false;
                    //sweepParams.Attenuation = 0; // change attenuation back to 0, because I don't know the initial setting, I hardcode to zero
                    DetectSegment(sweepParams, fftParams, powerList,
                        frequencyList, cf, numFftsToCopy, ref overload);
                    if (overload)
                    {
                        dataMessage.overloadFlag = true;
                    }
                }
            }

            List<double> measuredPowers = new List<double>();
            // Init antenna class to access cable loss and antenna gain
            string antennaString =
                File.ReadAllText(Constants.AntennaFile);
            SysMessage.Antenna antenna = 
                new JavaScriptSerializer().Deserialize<SysMessage.Antenna>(
                    antennaString);

            // reference power levels to input of isotropic antenna 
            for (int i = 0; i < powerList.Count; i++)
            {
                measuredPowers.Add(
                    powerList[i] + antenna.cableLoss - antenna.gain - 
                    yFactorCal.GainDbw[i]);
            }
            dataMessage.processed = true;
            dataMessage.measuredPowers = measuredPowers;
            return false;
        }
        #endregion

        #region private methods 
        private bool DetectSpan(FFTParams fftParams, SweepParams measParams,
            List<double> powerList, List<double> frequencies, 
            List<double> attenList)
        {
            // detect over span
            for (int i = 0; i < fftParams.NumSegments; i++)
            {
                double cf = fftParams.CenterFrequencies[i];

                uint numFftsToCopy;
                if (i == fftParams.NumFullSegments)
                {
                    numFftsToCopy = fftParams.NumBinsLastSegment;
                }
                else
                {
                    numFftsToCopy = fftParams.NumValidFftBins;
                }

                bool overload = false;
                bool err = DetectSegment(measParams, fftParams, powerList, 
                    null, cf, numFftsToCopy, ref overload);
                if (err)
                {
                    return true;
                }
            }
            return false;
        }

        private bool DetectSegment(SweepParams measParams, 
            FFTParams fftParams, List<double> powerList,
            List<double> frequencies, double cf, uint numFftsToCopy,
            ref bool overloadFlag)
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
            int startIndex = 0;

            while (!dataRetrieved)
            {
                TimeSpan elapsedTime = DateTime.Now.Subtract(t0);
                if (elapsedTime.Milliseconds > maxDataReadMilliSeconds)
                {
                    Utilites.LogMessage("Getting segment data timed out, " +
                        "restarting cal");
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
                        // get the data 
                        // check if need to remove anti aliasing
                        // need startIndex after the while loop so declared outside 
                        if (measParams.RemoveAntiAliasing)
                        {
                            startIndex = GetStartIndex(fftParams.SampleRate,
                                (int)fftParams.NumFftBins);
                        }

                        FloatArrayToListOfDoubles(frequencyData, 
                        powerList, numFftsToCopy, startIndex);
                        // check if overload occured 
                        if (dataHeader.overload != 0)
                        {
                            overloadFlag = true;
                        }
                        else
                        {
                            overloadFlag = false;
                        }
                        dataRetrieved = true;
                        break;

                    case AgSalLib.SalError.SAL_ERR_NO_DATA_AVAILABLE:
                        // data is not available yet ... 
                        break;
                    default:
                        SensorError(err, "salGetSegmentData");
                        break;
                }
            }

            if (frequencies != null)
            {
                // calculate frquencies
                for (int i = 0; i < numFftsToCopy; i++)
                {
                    frequencies.Add(dataHeader.startFrequency +
                        (startIndex - 1) * dataHeader.frequencyStep +
                        i * dataHeader.frequencyStep);
                }
            }

            return false;
        }

        public bool ValidAtten(double atten)
        {
            if (atten < MIN_ATTEN || atten > MAX_ATTEN)
            {
                Utilites.LogMessage("Invalid attenuation in input file");
                return false;
            }
            return true;
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
            else if (sys2Detect.ToLower().Equals("asr"))
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
        private int GetStartIndex(double sampleRate, int numBinsInFft)
        {
            int numValidBins = (int)Utilites.floorEven(numBinsInFft / 
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
