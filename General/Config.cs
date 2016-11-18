using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace General
{
    public class Config
    {
        private string sensorHostName;
        private string sensorManagmentServerIp;
        private string preselectorIp;
        private string version;
        private string byteOrder;
        private string dataType;
        private string compression;
        private int sensorKey;

        public string SensorHostName
        {
            get { return sensorHostName; }
            set { sensorHostName = value; }
        }

        public string SensorManagmentServerIp
        {
            get { return sensorManagmentServerIp; }
            set { sensorManagmentServerIp = value; }
        }

        public string PreselectorIp
        {
            get { return preselectorIp; }
            set { preselectorIp = value; }
        }

        public string Version
        {
            get { return version; }
            set { version = value; }
        }

        public string ByteOrder
        {
            get { return byteOrder; }
            set { byteOrder = value; }
        }

        public string Compression
        {
            get { return compression; }
            set { compression = value; }
        }

        public string DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        public int SensorKey
        { 
            get { return sensorKey; }
            set { sensorKey = value; }
        }
      }
}
