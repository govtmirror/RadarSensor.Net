using System;
using System.IO;
using Newtonsoft.Json.Linq;
using AgSal;
using Logging;

namespace AgilentN6841A
{
    class SensorDriver
    {
        
        // sensorHandle is used to talk to a specific sensor
        private IntPtr smsHandle = IntPtr.Zero; // handle to the Sensor Management Server
        private IntPtr sensorHandle = IntPtr.Zero; // handle to the sensor
        private IntPtr measHandle = IntPtr.Zero; // handle to the sweep measurement

        // measurement paramater data structures 
        private AgSalLib.SweepParms sweepParams;
        private AgSalLib.TunerParms tunerParams;

        // loads sweepParams and turnerParams from JSON file
        JObject measurementParams;

        private AgSalLib.SensorCapabilities sensorCapabilities;

        private string smsHostName;

        public SensorDriver(String ip)
        {
            bool connectionPassed = ConnectSensor();
            if (!connectionPassed)
            {
                Console.WriteLine("Connecting to sensor failed");
                Environment.Exit(0); 
            }
            AgSalLib.salSensorBeep(sensorHandle);
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

        public void initSensorParams()
        {
            sweepParams = new AgSalLib.SweepParms();
            tunerParams = new AgSalLib.TunerParms();

        }

        public void SensorStartSweep()
        {

        }

        private void loadParamsFromJson()
        {
            sweepParams = new AgSalLib.SweepParms();
            tunerParams = new AgSalLib.TunerParms();

            // get mPar JSON file
            string file = @"C:\GitHub\RadarSensor\AgilentN6841A\mPar\sweep1.json";
            StreamReader stream = new StreamReader(file);
            string jSonString = stream.ReadToEnd();
            measurementParams = JObject.Parse(jSonString);
        }

        public static void Main(String[] args)
        {
            SensorDriver sensor = new SensorDriver("10.6.6.14");
            //sensor.SensorStartSweep();
            //Logger.logMessage("test");
            Console.Read();
        }
    }
}
