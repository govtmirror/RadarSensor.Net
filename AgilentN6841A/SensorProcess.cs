using System;
using System.IO;
using General;
using System.Web.Script.Serialization;
using System.Diagnostics;

namespace AgilentN6841A
{
    public class SensorProcess
    {
        internal class TimedCount
        {
            public DateTime start = DateTime.Now;

            internal void reset()
            {
                start = DateTime.Now;
            }
            internal double elaspedTime()
            {
                TimeSpan et = DateTime.Now.Subtract(start);
                return et.TotalSeconds;
            }
        }

        public const int SECONDS_IN_HOUR = 3600;

        /// <summary>
        /// 
        /// </summary>
        public static void MainLoop()
        {
            // verify needed paths exists 
            if (!Directory.Exists(Constants.MESSAGE_FILES_DIR))
            {
                Directory.CreateDirectory(Constants.MESSAGE_FILES_DIR);
            }

            if (!Directory.Exists(Constants.LOG_FILE_DIR))
            {
                Directory.CreateDirectory(Constants.LOG_FILE_DIR);
            }

            JavaScriptSerializer serializer = new JavaScriptSerializer();
            Config config =
                serializer.Deserialize<Config>(
                    File.ReadAllText(Constants.ConfigFile));

            SensorDriver sensor = new SensorDriver(config.PreselectorIp,
                config.SensorHostName);

            TimedCount timer = new TimedCount();
            Stopwatch stopwatch = new Stopwatch();
            bool initialCalComplete = false;
            YfactorCal yFactorCal = null;
            int numOfMeasurements = 0;

            // create and write initial location message 
            string locString = File.ReadAllText(Constants.LocMessage);
            LocMessage locMessage =
                serializer.Deserialize<LocMessage>(locString);
            locMessage.loadMessageFields();
            Utilites.WriteMessageToFile(locMessage);

            while (true)
            {
                if (timer.elaspedTime() >= SECONDS_IN_HOUR ||
                    !initialCalComplete)
                {
                    // reset stopwatch to zero but do not start 
                    stopwatch.Reset();
                    // read in parameters for calibration
                    SweepParams calParams;
                    string jsonString =
                        File.ReadAllText(Constants.Spn43CalSweepParamsFile);
                    calParams =
                        serializer.Deserialize<SweepParams>(
                            jsonString);

                    SysMessage sysMessage = new SysMessage();
                    sysMessage.loadMessageFields();
                    sysMessage.version = config.Version;
                    sysMessage.calibration.byteOrder = config.ByteOrder;
                    sysMessage.calibration.compression = config.Compression;
                    sysMessage.calibration.dataType = config.DataType;
                    sysMessage.calibration.numOfMeasurementsPerCal =
                        numOfMeasurements;
                    sysMessage.calibration.measurementType =
                        calParams.MeasurementType;
                    sysMessage.calibration.calsPerHour =
                        Constants.CALS_PER_HOUR;
                    sysMessage.calibration.compression =
                        Constants.COMPRESSION;
                    sysMessage.calibration.byteOrder =
                        Constants.BYTE_ORDER;
                    sysMessage.calibration.dataType =
                        Constants.DATA_TYPE;
                    sysMessage.calibration.measurementParameters.detector =
                        calParams.Detector;
                    sysMessage.calibration.measurementParameters.window =
                        calParams.Window;
                    sysMessage.calibration.measurementParameters.attenuation =
                        calParams.Attenuation;
                    sysMessage.calibration.measurementParameters.videoBw = -1;
                    sysMessage.calibration.measurementParameters.dwellTime =
                        calParams.DwellTime;

                    // if yFactorCall == null and error occured while performing cal
                    sensor.PerformCal(calParams, sysMessage, out yFactorCal);
                    if (yFactorCal == null)
                    {
                        Utilites.LogMessage("Error performing calibration, " +
                            "cal aborted");
                        // unhandled exception event handle will catch exception
                        throw new Exception("Error performing cal");
                    }
                    Utilites.WriteMessageToFile(sysMessage);
                    initialCalComplete = true;
                    timer.reset();
                    numOfMeasurements = 0;
                }
                else
                {
                    // need to have completed cal to perform sweep
                    if (yFactorCal == null) { continue; }

                    // get last time from stop watch 
                    TimeSpan elapsedTime = stopwatch.Elapsed;
                    stopwatch.Restart();

                    SweepParams sweepParams;
                    string jsonString =
                        File.ReadAllText(Constants.Spn43MeasurementFile);
                    sweepParams =
                        serializer.Deserialize<SweepParams>(
                            jsonString);

                    DataMessage dataMessage = new DataMessage();
                    dataMessage.loadMessageFields();
                    dataMessage.version = config.Version;
                    dataMessage.byteOrder = config.ByteOrder;
                    dataMessage.dataType = config.DataType;
                    dataMessage.comment = config.Compression;
                    dataMessage.timeBetweenAcquisitions =
                        elapsedTime.TotalSeconds;
                    dataMessage.sysToDetect = sweepParams.sys2Detect;
                    dataMessage.measurementType = sweepParams.MeasurementType;
                    dataMessage.compression = Constants.COMPRESSION;
                    dataMessage.dataType = Constants.DATA_TYPE;
                    dataMessage.byteOrder = Constants.BYTE_ORDER;
                    dataMessage.measurementParameters.attenuation =
                        sweepParams.Attenuation;
                    dataMessage.measurementParameters.detector =
                        sweepParams.Detector;
                    dataMessage.measurementParameters.dwellTime =
                        sweepParams.DwellTime;
                    dataMessage.measurementParameters.window =
                        sweepParams.Window;

                    bool err = sensor.PerformMeasurement(sweepParams,
                        dataMessage, yFactorCal);
                    if (err)
                    {
                        throw new Exception("Error performing measurement");
                    }

                    numOfMeasurements++;
                    Utilites.WriteMessageToFile(dataMessage);
                }
            }
        }
    }
}
