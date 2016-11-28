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

        public int SensorKey
        {
            get { return sensorKey; }
            set { sensorKey = value; }
        }
      }
}
