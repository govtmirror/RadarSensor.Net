using System;
using System.Collections.Generic;
using System.Linq;
using JsonClasses;
using Logging;

namespace AgilentN6841A
{
    public class Yfactor
    {
        private double[] noiseFigureDbw;
        private double[] gainDbw;
        private double[] meanPowerDbm;

        public Yfactor(List<double> ndOn, List<double> ndOff,
            SysMessage sysMessage)
        {
            noiseFigureDbw = new double[ndOff.Count];
            gainDbw = new double[ndOff.Count];
            meanPowerDbm = new double[ndOff.Count];

            double enrW; // excess noise ratio in watts
            enrW = DbmToWatts(sysMessage.preselector.excessNoiseRatio);
            double enbw = (double)sysMessage.calibration.
                measurementParameters.equivalentNoiseBw;

            for (int i = 0; i < ndOn.Count; i++)
            {
                // convert from logarithmic to linear units
                double wattsNdOff = DbmToWatts(ndOff[i]);
                double wattsNdOn = DbmToWatts(ndOn[i]);

                // calculate noise ratio 
                double y = wattsNdOn / wattsNdOff;

                // calculate noise figure
                double noiseFigure = enrW / (y - 1);
                noiseFigureDbw[i] = WattsToDbw(noiseFigure);

                // calculate gain
                double gain = wattsNdOn /
                    (1.38e-23 * (25 + 273.15) * enbw *
                    (enrW + noiseFigure));
                gainDbw[i] = WattsToDbw(gain);

                // calculate mean power of receiver noise (Watts) 
                double meanPwr = 1.38e-23 * (25 + 273.15) *
                    enbw * noiseFigure * gain;
                meanPowerDbm[i] = WattsToDbm(meanPwr);
            }

        }

        #region properties
        public double[] NoseFigure
        {
            get { return noiseFigureDbw; }
        }

        public double[] Gain
        {
            get { return gainDbw; }
        }
        #endregion

        public double[] MeanPower
        {
            get { return meanPowerDbm; }
        }

        #region static utility methods
        /// <summary>
        /// converts dmb to watts
        /// </summary>
        /// <param name="p">power in dbm</param>
        /// <returns>power in watts</returns>
        public static double DbmToWatts(double p)
        {
            return Math.Pow(10, ((p - 30) / 10));
        }

        public static double WattsToDbm(double p)
        {
            return 10 * Math.Log10(p) + 30;
        }

        public static double WattsToDbw(double p)
        {
            return 10 * Math.Log10(p);
        }
        #endregion

        public static void Main(string[] args) { }
    }
}