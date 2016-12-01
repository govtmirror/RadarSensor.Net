using System.ServiceProcess;
using System;
using General;
using System.IO;
using System.Diagnostics;

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
            StartSensorProcess();
        }

        public void OnDebug()
        {
            OnStart(null);
        }

        protected override void OnStart(string[] args)
        {
            Utilites.LogMessage("Radar Sensor Service started");
            base.OnStart(args);
            // Set up a timer to trigger every minute.
            System.Timers.Timer timer = new System.Timers.Timer();
            timer.Interval = 600000; // 10 minutes
            timer.Elapsed += new System.Timers.ElapsedEventHandler(this.OnTimer);
            timer.Start();
        }

        protected override void OnStop()
        {
            Utilites.LogMessage("Radar Sensor Service stopped by the user");
            base.OnStop();
        }

        private void OnTimer(object sender, System.Timers.ElapsedEventArgs args)
        {
            // TODO: Insert monitoring activities here.
            Process[] processList =
                Process.GetProcessesByName(Constants.SENSOR_PROCESS_NAME);
            if (processList.Length == 0)
            {
                StartSensorProcess();
            }
        }

        private void StartSensorProcess()
        {
            string exePath =
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                Constants.SENSOR_PROCESS_NAME + ".exe");
            ProcessStartInfo info = new ProcessStartInfo(exePath);
            info.CreateNoWindow = true;
            info.UseShellExecute = false;
            Process.Start(info);
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
#if DEBUG
            //RadarSensorService sensorService = new RadarSensorService();
            //sensorService.OnDebug();
            //Thread.Sleep(System.Threading.Timeout.Infinite);
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[] { new RadarSensorService() };
            ServiceBase.Run(servicesToRun);
#else
            ServiceBase[] servicesToRun;
            servicesToRun = new ServiceBase[] { new RadarSensorService() };
            ServiceBase.Run(servicesToRun);
#endif 
        }
    }
}