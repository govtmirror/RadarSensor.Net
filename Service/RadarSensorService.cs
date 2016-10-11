using AgilentN6841A;
using SensorFrontEnd;
using Logging;
using System.ServiceProcess;
using System.Threading;
using System.Collections.Generic;
using System;

namespace Service
{
    partial class RadarSensorService : ServiceBase
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

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
            SensorDriver sensor = new SensorDriver("10.6.6.14");
            Preselector preselector  = new Preselector("10.6.6.22");

            List<float> powerList = new List<float>();
            List<float> attenList = new List<float>();

            // perform sweep 
            sensor.SensorPerfromSweep(AgilentN6841A.Mband.SPN43, 
                preselector, ref powerList, ref attenList);
            Console.ReadLine();

            // create JSON metadata file 
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
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

