using System;
using System.Text;
using System.IO;

namespace General
{
    public static class Constants
    {
        // senser managment server ip address
        public static readonly string SMS_IP = "10.6.6.14";

        public static readonly string SENSOR_HOST_NAME = "RM3420B";

        // web relay ip address 
        public static readonly string PRESELECTOR_IP = "10.6.6.22";

        // path to store Messages as JSON file
        public static readonly string MESSAGE_FILES_DIR
            = @"C:\SpectrumMonitoring\Data";

        // path to log file
        public static readonly string LOG_FILE_DIR =
           @"C:\RadarSensor";

        public static readonly string TRANSFER_SPEC_VER =
            "1.0.16";

        private static string currentDir =
    Directory.GetCurrentDirectory();

        // files that will be deserealized into a class at runtime
        private static readonly string JSON_FILES_Dir = "jsonFiles";

        // dir with csv files for unit tests
        private static readonly string
            UNIT_TEST_VALUES = "unitTestExpectedValues";

        public static string LogFile
        {
            get
            {
                return Path.Combine(LOG_FILE_DIR, 
                    "radarSensorLog.txt");
            }
        }

        public static string SysMessageFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "SysMessage.json");
            }
        }

        public static string DataMessageFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "DataMessage.json");
            }
        }

        public static string LocMessage
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "LocMessage.json");
            }
        }

        public static string AntennaFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "Antenna.json");
            }
        }

        public static string CalibrationFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "Calibration.json");
            }
        }

        public static string CotsSensorFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "CotsSensor.json");
            }
        }

        public static string PreselectorFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "Preselector.json");
            }
        }

        public static string Spn43CalSweepParamsFile
        {
            get
            {
                return Path.Combine(currentDir, 
                    JSON_FILES_Dir, "spn43Cal.json");
            }
        }

        public static string Spn43MeasurementFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES_Dir, "spn43Sweep.json");
            }
        }

        // Expected value files for unit tests
        public static string CenterFrequencyValues
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TEST_VALUES, "centerFrequenciesExpectedValues.csv");
            }
        }

        public static string FrequeencyValues
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TEST_VALUES, "frequencyListExpectedValues.csv");
            }
        }

        public static string NoiseDiodeOffInputs
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TEST_VALUES, "ndOffInputValues.csv");
            }
        }

        public static string NoiseDiodeOnInputs
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TEST_VALUES, "ndOnInputValues.csv");
            }
        }

        public static string ExpectedNoiseFigure
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TEST_VALUES, "expectedNoiseFigure.csv");
            }
        }

        public static string ExcpectedGain
        {
            get
            {
                return Path.Combine(currentDir,
                    UNIT_TEST_VALUES, "expectedGain.csv");
            }
        }
    }
}
