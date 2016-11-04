using AgilentN6841A;
using SensorFrontEnd;
using System.ServiceProcess;
using System.Threading;
using System.Collections.Generic;
using System;
using System.Web.Script.Serialization;
using System.IO;
using General;

namespace Service
{
    partial class RadarSensorService : ServiceBase
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;
        public const int SECONDS_IN_HOUR = 3600;

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

        public RadarSensorService()
        {
            this.ServiceName = "RadarSensorService";
            this.CanPauseAndContinue = true;
            this.CanStop = true;
            Thread sensorThread = new Thread(this.mainThread);
            sensorThread.Start();
        }

        protected override void OnStart(string[] args)
        {
            Utilites.LogMessage("Radar Sensor Service started");
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            Utilites.LogMessage("Radar Sensor Service stopped by the user");
            base.OnStop();
        }

        /// <summary>
        /// 
        /// </summary>
        private void mainThread()
        {
            // verify needed paths exits 
            if (!Directory.Exists(Constants.MESSAGE_FILES_DIR))
            {
                Directory.CreateDirectory(Constants.MESSAGE_FILES_DIR);
            }

            if (!Directory.Exists(Constants.LOG_FILE_DIR))
            {
                Directory.CreateDirectory(Constants.LOG_FILE_DIR);
            }

            SensorDriver sensor = new SensorDriver();
            TimedCount timer = new TimedCount();
            bool initialCalComplete = false;
            YfactorCal yFactorCal = null;
            int numOfMeasurements = 0;

            // create and write initial location message 
            string locString = File.ReadAllText(Constants.LocMessage);
            LocMessage locMessage = 
                new JavaScriptSerializer().Deserialize<LocMessage>(locString);
            locMessage.loadMessageFields();
            Utilites.WriteMessageToFile(locMessage);

            while (true)
            {
                if (timer.elaspedTime() >= SECONDS_IN_HOUR ||
                    !initialCalComplete)
                {    
                    //Perform calibration
                    // read in parameters for calibration
                    SweepParams calParams;
                    string jsonString = 
                        File.ReadAllText(Constants.Spn43CalSweepParamsFile);
                    calParams =
                        new JavaScriptSerializer().Deserialize<SweepParams>(
                            jsonString);

                    SysMessage sysMessage = new SysMessage();
                    sysMessage.loadMessageFields();

                    sensor.PerformCal(calParams, sysMessage, out yFactorCal);
                    if (yFactorCal == null)
                    {
                        Utilites.LogMessage("Error performing calibration, " +
                            "cal aborted");
                        // do not write SysMessage
                        continue;
                    }
                    Utilites.WriteMessageToFile(sysMessage);
                    //Console.ReadLine();
                    initialCalComplete = true;
                    timer.reset();
                    numOfMeasurements = 0;
                }
                else
                {
                    if (yFactorCal == null) { continue; }

                    // perform measurement
                    DataMessage dataMessage = new DataMessage();
                    SweepParams sweepParams;
                    string jsonString =
                        File.ReadAllText(Constants.Spn43MeasurementFile);
                    sweepParams =
                        new JavaScriptSerializer().Deserialize<SweepParams>(
                            jsonString);
                    sensor.performMeasurement(sweepParams, dataMessage, yFactorCal);
                }          
            }
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; 
        /// otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        public static void Main(string[] args)
        {
            RadarSensorService service = new RadarSensorService();
        }
    }
}

