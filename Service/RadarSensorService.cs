using AgilentN6841A;
using SensorFrontEnd;
using Logging;
using System.ServiceProcess;
using System.Threading;
using System.Collections.Generic;
using System;
using JsonClasses;
using System.Web;
using System.IO;
using System.Timers;
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
            Logger.logMessage("Radar Sensor Service started");
            base.OnStart(args);
        }

        protected override void OnStop()
        {
            Logger.logMessage("Radar Sensor Service stopped by the user");
            base.OnStop();
        }

        /// <summary>
        /// 
        /// </summary>
        private void mainThread()
        {
            SensorDriver sensor = new SensorDriver();
            Preselector preselector  = new Preselector(Constants.PRESELECTOR_IP);
            TimedCount timer = new TimedCount();

            while (true)
            {
                if (timer.elaspedTime() >= SECONDS_IN_HOUR)
                {
                    timer.reset();
                    //Perform calibration
                }
                else
                {
                    // perform measurement
                }

                // read in parameters for calibration
                SweepParams measParams;
                string json = File.ReadAllText(Constants.Spn43CalSweepParamsFile);
                measParams =
                    new System.Web.Script.Serialization.
                    JavaScriptSerializer().Deserialize<SweepParams>(
                        json);

                JsonClasses.SysMessage sysMessage = new SysMessage();

                // create and write location message 
                string locString = File.ReadAllText(Constants.LocMessage);
                LocMessage locMessage = new System.Web.Script.Serialization.
                JavaScriptSerializer().Deserialize<LocMessage>(locString);
                locMessage.sensorId = Constants.SENSOR_HOST_NAME;
                // get epoch time 
                TimeSpan epochTime =
                DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1));
                locMessage.time = (long)epochTime.TotalSeconds;

                sysMessage.calibration.temp = preselector.getTemp();

                // perform calibration
                sensor.performCal(measParams, sysMessage, preselector);

                Console.ReadLine();
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

