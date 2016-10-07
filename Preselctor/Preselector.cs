using System;
using System.Net;
using System.IO;
using Logging;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;
namespace Preselctor
{
    public class Preselector
    {
        private X300 webRelay;

        public Preselector(string ip)
        {
            webRelay = new X300(ip);
        }

        public void setRF1Source()
        {
            bool success = webRelay.setRelayState(3, 0);
        }

        public void setRF2Source()
        {
            bool success = webRelay.setRelayState(4, 0);
        }

        /// <summary>
        /// Sets ND to 700 Mhz filter
        /// </summary>
        public void setRF1Nd()
        {
            powerOnNd();
            bool sw2 = webRelay.setRelayState(2, 0);
            bool sw3 = webRelay.setRelayState(3, 1);
        }

        /// <summary>
        /// Sets ND to 3.5 Ghz filter 
        /// </summary>
        public void setRF2Nd()
        {
            powerOnNd();
            bool sw2 = webRelay.setRelayState(2, 1);
            bool sw4 = webRelay.setRelayState(4, 1);
        }

        public void getTemp()
        {
            //return webRelay.getTemp();
        }

        /// <summary>
        /// applies power to noise diode
        /// </summary>
        private void powerOnNd()
        {
            bool success = webRelay.setRelayState(1, 1);
        }

        private void powerOffNd()
        {
            bool sucess = webRelay.setRelayState(1, 0);
        }

        public static void Main(string[] args)
        {
            Preselector p = new Preselector("10.6.6.22");
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
    