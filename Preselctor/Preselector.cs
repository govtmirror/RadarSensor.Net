using System;
using System.Net;
using System.IO;
using Logging;
using System.Threading;
using System.Text.RegularExpressions;

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

        }

        public void setRF2Source()
        {

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
                StreamReader sr = new StreamReader(response.GetResponseStream());
                string responseString = sr.ReadToEnd();
                return validRelayState(relay, state, responseString);
            }

            internal bool validRelayState(int relay, int state, string relays)
            {
                string searchString = "<relay" + relay + "state>"
                    + state + "</relay" + relay + "state>";
                if (!relays.Contains(searchString))
                {
                    Console.WriteLine("Relay state not changed");
                    return false;
                }
                return true;
            }

            internal static void Main(string[] args)
            {
                X300 webRelay = new X300("10.6.6.22");

                webRelay.setRelayState(4, 1);
                Thread.Sleep(200);
                webRelay.setRelayState(4, 0);
                Thread.Sleep(200);
                webRelay.setRelayState(3, 1);
                Thread.Sleep(200);
                webRelay.setRelayState(3, 0);
                Console.Read();
            }
        }

        public static void Main(string[] args)
        {
            Preselector p = new Preselector("10.6.6.22");
        }
    }
}
    