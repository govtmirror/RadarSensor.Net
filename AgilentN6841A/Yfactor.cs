using System;
using System.Collections.Generic;
using System.Linq;
using JsonClasses;
using Logging;

namespace AgilentN6841A
{
    public class Yfactor
    {
        // Boltzmann's constant
        public readonly double K = 1.38e-23;
        // IEEE standard reference temperature in Kelvin
        public readonly double T0 = 290;


        private double[] noiseFigureDbw;
        private double[] gainDbw;
        private double[] meanPowerDbm;

        private double noiseFigureDbwAvg;
        private double gainDbwAvg;

        private double[] noiseFigureWatts;
        private double[] gainWatts;

        public Yfactor(List<double> ndOn, List<double> ndOff,
            double excessNoiseRatio, double enbw)
        {
            noiseFigureDbw = new double[ndOff.Count];
            gainDbw = new double[ndOff.Count];
            meanPowerDbm = new double[ndOff.Count];

            gainWatts = new double[ndOff.Count];
            noiseFigureWatts = new double[ndOff.Count];

            noiseFigureDbwAvg = 0.0;
            gainDbwAvg = 0.0;

            // excess noise ratio in watts
            double enrW = DbwToWatts(excessNoiseRatio);
            
            for (int i = 0; i < ndOn.Count; i++)
            {
                // convert from logarithmic to linear units
                double wattsNdOff = DbmToWatts(ndOff[i]);
                double wattsNdOn = DbmToWatts(ndOn[i]);

                // calculate noise ratio 
                double y = wattsNdOn / wattsNdOff;

                // calculate noise figure
                double noiseFigure = enrW / (y - 1);
                noiseFigureWatts[i] = noiseFigure;
                noiseFigureDbw[i] = WattsToDbw(noiseFigure);
                // sum nosie figure for avg
                noiseFigureDbwAvg += noiseFigureDbw[i];

                // calculate gain
                double gain = wattsNdOn /
                    (this.K * T0 * enbw * (enrW + noiseFigure));
                gainWatts[i] = gain;
                gainDbw[i] = WattsToDbw(gain);
                // sum gain for average
                gainDbwAvg += gainDbw[i];

                // calculate mean power of receiver noise (Watts) 
                double meanPwr = 1.38e-23 * (25 + 273.15) *
                    enbw * noiseFigure * gain;
                meanPowerDbm[i] = WattsToDbm(meanPwr);
                }
            
            gainDbwAvg /= GainDbw.Length;
            noiseFigureDbwAvg /= noiseFigureDbw.Length;
        }

        #region properties
        public double[] NoseFigureDbw
        {
            get { return noiseFigureDbw; }
        }

        public double[] GainDbw
        {
            get { return gainDbw; }
        }

        public double[] MeanPowerDbm
        {
            get { return meanPowerDbm; }
        }

        public double[] NoiseFigureWatts
        {
            get { return noiseFigureWatts; }
        }

        public double[] GainWatts
        {
            get { return gainWatts; }
        }

        public double GainDbwAvg
        {
            get { return gainDbwAvg; }
        }

        public double NoiseFigureDbwAvg
        {
            get { return noiseFigureDbwAvg; }
        }
        #endregion

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

        /// <summary>
        /// converts watts to dbm
        /// </summary>
        /// <param name="p"></param>
        /// <returns>power in dbm</returns>
        public static double WattsToDbm(double p)
        {
            return 10 * Math.Log10(p) + 30;
        }

        /// <summary>
        /// converts watts to dbw
        /// </summary>
        /// <param name="p"></param>
        /// <returns>power in dbw</returns>
        public static double WattsToDbw(double p)
        {
            return 10 * Math.Log10(p);
        }

        /// <summary>
        /// converts dbw to watts
        /// </summary>
        /// <param name="p"></param>
        /// <returns>power in watts</returns>
        public static double DbwToWatts(double p)
        {
            return Math.Pow(10, p / 10);
        }
        #endregion

        public static void Main(string[] args) { }
    }
}