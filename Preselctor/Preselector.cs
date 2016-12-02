using System;
using System.Net;
using System.IO;
using General;
using System.Threading;
using System.Text.RegularExpressions;
using System.Xml;

namespace SensorFrontEnd
{
    public class Preselector
    {
        private WebRelay webRelay;
        /// <summary>
        /// excess noiose ratio of noise diode in Preselector
        /// </summary>

        public Preselector(string ip)
        {
            webRelay = new WebRelay(ip);
        }

        /// <summary>
        /// powers on noide diode
        /// </summary>
        public void PowerOnNd()
        {
            webRelay.SetRelayState(1, 1);
        }

        /// <summary>
        /// powers off noide diode
        /// </summary>
        public void PowerOffNd()
        {
            webRelay.SetRelayState(1, 0);
        }

        /// <summary>
        /// set source to RF in 
        /// </summary>
        public void SetRfIn()
        {
            webRelay.SetRelayState(2, 0);
        }

        /// <summary>
        /// sets source to noide diode for calibration
        /// </summary>
        public void SetNdIn()
        {
            webRelay.SetRelayState(2, 1);
        }

        /// <summary>
        /// Sets path to 3.5G filter
        /// </summary>
        /// <param name="filter"></param>
        public void Set3_5Filter()
        {
            webRelay.SetRelayState(4, 0);
        }

        /// <summary>
        /// Sets path to 3.0G filter
        /// </summary>
        /// <returns></returns>
        public void Set3_0Filter()
        {
            webRelay.SetRelayState(4, 1);
        }

        /// <summary>
        /// bypasses filters for 2.8G band
        /// </summary>
        public void SetBypass()
        {
            webRelay.SetRelayState(3, 1);
        }
    
        public double GetTemp()
        {
            return webRelay.GetTemp();
        }

        public static void Main(string[] args)
        {
        }
    }

    /// <summary>
    /// controlls web relay
    /// </summary>
    internal class WebRelay
    {
        private string baseUrl;
        private string ctlUrl;
        private int port = 80; // default port for X300

        internal WebRelay(string baseURL)
        {
            Regex ipMatcher = new Regex("^(?:[0-9]{1,3}\\.){3}[0-9]{1,3}$");

            if (baseURL.StartsWith("http://"))
            {
                string temp = baseURL.Substring(7);
                ValidateIP(ipMatcher, temp);
                this.baseUrl = baseURL;
            }
            else
            {
                ValidateIP(ipMatcher, baseURL);
                baseUrl = "http://" + baseURL;
            }
            ctlUrl = baseUrl + ":" + port + "/state.xml";
        }

        internal void ValidateIP(Regex ipMatcher, string ip)
        {
            if (!ipMatcher.IsMatch(ip))
            {
                Console.WriteLine("invalid ip");
                Utilites.LogMessage("X300 relay initialized with invalid ip address");
                Environment.Exit(0);
            }
        }

        internal bool SetRelayState(int relay, int state)
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
                    Utilites.LogMessage("relay in invalid state");
                    return false;
                }
            }
            else
            {
                Utilites.LogMessage("Invalid XML when verifiyng relay " +
                    "was changed to correct state");
                return false;
            }
            return true;
        }

        /// <summary>
        /// gets temperatur from preselctor probe at sensor1
        /// </summary>
        /// <returns> current temperatre or double.MinValue if error 
        /// </returns>
        internal double GetTemp()
        {
            string rqstUrl = ctlUrl;
            WebRequest wr = WebRequest.Create(rqstUrl);
            WebResponse response = wr.GetResponse();
            Stream responseStream = response.GetResponseStream();
            XmlTextReader xmlReader = new XmlTextReader(responseStream);
            double temp;
            if (xmlReader.ReadToFollowing("sensor1"))
            {
                temp = xmlReader.ReadElementContentAsDouble();
            }
            else
            {
                Utilites.LogMessage("Invalid XML when reading " +
                    "temperature from sensor1");
                temp = double.MinValue;
            }
            return temp;
        }

        //fahrenheit to kelvin
        private double f2k(double f)
        {
            return (f + 459.67f) * 5.0f / 9.0f;
        }

        public static void Main(string[] args)
        {
            WebRelay webRelay = new WebRelay("10.6.6.22");

            webRelay.SetRelayState(4, 1);
            Thread.Sleep(200);
            webRelay.SetRelayState(4, 0);
            Thread.Sleep(200);
            webRelay.SetRelayState(3, 1);
            Thread.Sleep(200);
            webRelay.SetRelayState(3, 0);
            webRelay.GetTemp();
            Console.Read();
        }
    }
}
    