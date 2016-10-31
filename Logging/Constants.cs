using System;
using System.Text;
using System.IO;

namespace General
{
    public static class Constants
    {
        public static readonly string SMS_IP = "10.6.6.14";
        public static readonly string SENSOR_HOST_NAME = "RM3420B";

        public static readonly string PRESELECTOR_IP = "10.6.6.22";

        private static string currentDir =
    Directory.GetCurrentDirectory();

        private static readonly string JSON_FILES = "jsonFiles";

        private static readonly string
            UNIT_TEST_VALUES = "unitTestExpectedValues";


        public static string SysMessageFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES, "SysMessage.json");
            }
        }

        public static string DataMessageFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES, "DataMessage.json");
            }
        }

        public static string LocMessage
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES, "LocMessage.json");
            }
        }

        public static string AntennaFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES, "Antenna.json");
            }
        }

        public static string CotsSensorFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES, "CotsSensor.json");
            }
        }

        public static string PreselectorFile
        {
            get
            {
                return Path.Combine(currentDir,
                    JSON_FILES, "Preselector.json");
            }
        }

        public static string Spn43CalSweepParamsFile
        {
            get
            {
                return Path.Combine(currentDir, 
                    JSON_FILES, "spn43Cal.json");
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
    }
}
