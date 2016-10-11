using System;
using System.Net;
using System.IO;
using Logging;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;

namespace SensorFrontEnd
{
    public class Preselector
    {
        private X300 webRelay;

        public Preselector(string ip)
        {
            webRelay = new X300(ip);
        }

        /// <summary>
        /// powers on noide diode
        /// </summary>
        public void powerOnNd()
        {
            webRelay.setRelayState(1, 1);
        }

        /// <summary>
        /// powers off noide diode
        /// </summary>
        public void powerOffNd()
        {
            webRelay.setRelayState(1, 0);
        }

        /// <summary>
        /// set source to RF in 
        /// </summary>
        public void setRfIn()
        {
            webRelay.setRelayState(1, 0);
        }

        /// <summary>
        /// sets source to noide diode for calibration
        /// </summary>
        public void setNdIn()
        {
            webRelay.setRelayState(1, 1);
        }

        public float getTemp()
        {
            return webRelay.getTemp();
        }

        public static void Main(string[] args)
        {
        }
    }

    internal class X300
    {
        private string baseUrl;
        private string ctlUrl;
        private int port = 80; // default port for X300

        internal X300(string baseURL)
        {
            Regex ipMatcher = new Regex("^(?:[0-9]{1,3}\\.){3}[0-9]{1,3}$");

            if (baseURL.StartsWith("http://"))
            {
                string temp = baseURL.Substring(7);
                validateIP(ipMatcher, temp);
                this.baseUrl = baseURL;
            }
            else
            {
                validateIP(ipMatcher, baseURL);
                baseUrl = "http://" + baseURL;
            }
            ctlUrl = baseUrl + ":" + port + "/state.xml";
        }

        internal void validateIP(Regex ipMatcher, string ip)
        {
            if (!ipMatcher.IsMatch(ip))
            {
                Console.WriteLine("invalid ip");
                Logger.logMessage("X300 relay initialized with invalid ip address");
                Environment.Exit(0);
            }
        }

        internal bool setRelayState(int relay, int state)
        {
            string rqstUrl = ctlUrl + "?relay" + relay + "State=" + state;
            WebRequest wr = WebRequest.Create(rqstUrl);
            WebResponse response = wr.GetResponse();
            Stream responseStream = response.GetResponseStream();
            XmlTextReader xmlReader = new XmlTextReader(responseStream);
            int relayState;
            if (xmlReader.ReadToFollowing("relay"+relay+"state"))
            {
                relayState = xmlReader.ReadElementContentAsInt();
                if (relayState != state)
                {
                    Logger.logMessage("relay in invalid state");
                    return false;
                }
            }
            else
            {
                Logger.logMessage("Invalid XML when verifiyng relay " +
                    "was changed to correct state");
                return false;
            }
            return true;
        }

        /// <summary>
        /// gets temperatur from preselctor probe at sensor1
        /// </summary>
        /// <returns> current temperatre or float.MinValue if error 
        /// </returns>
        internal float getTemp()
        {
            string rqstUrl = ctlUrl;
            WebRequest wr = WebRequest.Create(rqstUrl);
            WebResponse response = wr.GetResponse();
            Stream responseStream = response.GetResponseStream();
            XmlTextReader xmlReader = new XmlTextReader(responseStream);
            float temp;
            if (xmlReader.ReadToFollowing("sensor1"))
            {
                temp = xmlReader.ReadElementContentAsFloat();
            }
            else
            {
                Logger.logMessage("Invalid XML when reading " +
                    "temperature from sensor1");
                temp = float.MinValue;
            }
            return temp;
        }

        //fahrenheit to kelvin
        private float f2k(float f)
        {
            return (f + 459.67f) * 5.0f / 9.0f;
        }

        public static void Main(string[] args)
        {
            X300 webRelay = new X300("10.6.6.22");

            webRelay.setRelayState(4, 1);
            Thread.Sleep(200);
            webRelay.setRelayState(4, 0);
            Thread.Sleep(200);
            webRelay.setRelayState(3, 1);
            Thread.Sleep(200);
            webRelay.setRelayState(3, 0);
            webRelay.getTemp();
            Console.Read();
        }
    }
}
    