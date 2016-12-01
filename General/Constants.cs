using System;
using System.Text;
using System.IO;

namespace General
{
    public static class Constants
    {
        // senser managment server ip address
        public static readonly string SMS_IP = "10.6.6.13";

        public static readonly string SENSOR_HOST_NAME = "DKs";

        // web relay ip address 
        public static readonly string PRESELECTOR_IP = "10.6.6.12";

        // for sys and data messages 
        public static readonly int CALS_PER_HOUR = 1;
        public static readonly string BYTE_ORDER = "network";
        public static readonly string COMPRESSION = "none";
        public static readonly string DATA_TYPE = "ASCII";
        public static readonly string SENSOR_PROCESS_NAME = "AgilentN6841A";

        // path to store Messages as JSON file
        public static readonly string MESSAGE_FILES_DIR
            = @"C:\SpectrumMonitoring\Data";

        // path to log file
        public static readonly string LOG_FILE_DIR =
           @"C:\RadarSensor";

        public static readonly string TRANSFER_SPEC_VER =
            "1.0.16";

        private static string currentDir =
    AppDomain.CurrentDomain.BaseDirectory;

        // files that will be deserealized into a class at runtime
        private static readonly string CONFIG = "config";

        // dir with csv files for unit tests
        private static readonly string
            UNIT_TESTS = "unitTests";

        public static string LogFile
        {
            get
            {
                return Path.Combine(LOG_FILE_DIR, 
                    "radarSensorLog.txt");
            }
        }

        public static string ConfigFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "config.json");
            }
        }

        public static string SysMessageFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "SysMessage.json");
            }
        }

        public static string DataMessageFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "DataMessage.json");
            }
        }

        public static string LocMessage
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "LocMessage.json");
            }
        }

        public static string AntennaFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "Antenna.json");
            }
        }

        public static string CalibrationFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "Calibration.json");
            }
        }

        public static string CotsSensorFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "CotsSensor.json");
            }
        }

        public static string PreselectorFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "Preselector.json");
            }
        }

        public static string Spn43CalSweepParamsFile
        {
            get
            {
                return Path.Combine(currentDir, 
                    CONFIG, "spn43Cal.json");
            }
        }

        public static string Spn43MeasurementFile
        {
            get
            {
                return Path.Combine(currentDir,
                    CONFIG, "spn43Sweep.json");
            }
        }

        // Expected value files for unit tests
        public static string CenterFrequencyValues
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TESTS, "centerFrequenciesExpectedValues.csv");
            }
        }

        public static string FrequeencyValues
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TESTS, "frequencyListExpectedValues.csv");
            }
        }

        public static string NoiseDiodeOffInputs
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TESTS, "ndOffInputValues.csv");
            }
        }

        public static string NoiseDiodeOnInputs
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TESTS, "ndOnInputValues.csv");
            }
        }

        public static string ExpectedNoiseFigure
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TESTS, "expectedNoiseFigure.csv");
            }
        }

        public static string ExcpectedGain
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TESTS, "expectedGain.csv");
            }
        }
    }
}
