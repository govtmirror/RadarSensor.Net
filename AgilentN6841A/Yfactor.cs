using System;
using System.Collections.Generic;
using System.Linq;
using JsonClasses;

namespace AgilentN6841A
{
    public class Yfactor
    {
        private List<double> noiseFigure;
        private List<double> gain;
        private List<double> meanPower;

        public Yfactor(List<double> ndOff, List<double> ndOn,
            SweepParams measParams)
        {
            List<double> ndOffW = new List<double>();
            List<double> ndOnW = new List<double>();

            if (ndOff.Count == ndOn.Count)
            {
                for (int i = 0; i < ndOn.Count; i ++)
                {
                    ndOffW.Add(dbmToWatts(ndOff[i]));
                    ndOnW.Add(dbmToWatts(ndOn[i]));
                }
            }
        }

        #region properties
        public List<double> NoseFigure
        {
            get { return noiseFigure; }
        }

        public List<double> Gain
        {
            get { return gain; }
        }
        #endregion

        public List<double> MeanPower
        {
            get { return meanPower; }
        }

        #region static utility methods
        /// <summary>
        /// converts dmb to watts
        /// </summary>
        /// <param name="p">power in dbm</param>
        /// <returns>power in watts</returns>
        public static double dbmToWatts(double p)
        {
            return Math.Pow(10, ((p - 30) / 10));
        }
        #endregion

        public static void Main(string[] args)
        {
            Console.WriteLine(dbmToWatts(25));
            Console.ReadLine();
        }
    }
}
