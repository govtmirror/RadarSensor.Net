using System;
using System.Collections.Generic;
using General;

namespace AgilentN6841A
{
    public class YfactorCal
    {
        // Boltzmann's constant
        private double K = 1.38e-23;
        // IEEE standard reference temperature in Kelvin
        private double T0 = 290;

        private double[] noiseFigureDbw;
        private double[] gainDbw;
        private double[] meanPowerDbm;

        private double meanNoiseFigureDbw;
        private double meanGainDbw;
        private double meanDetectedSysNoise;

        public YfactorCal(List<double> ndOn, List<double> ndOff, 
            double rbw, double enbw, double dwellTime, 
            double excessNoiseRatio, double cableLoss, 
            double antennaGain)
        {
            noiseFigureDbw = new double[ndOff.Count];
            gainDbw = new double[ndOff.Count];
            meanPowerDbm = new double[ndOff.Count];

            double[] detectedSystemNoisePwr =
                new double[ndOff.Count];

            double pkToPkAvg = PeakToPeakAvg(dwellTime, rbw);

            meanNoiseFigureDbw = 0.0;
            meanGainDbw = 0.0;
            meanDetectedSysNoise = 0.0;

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
                double noiseFigureWatts = enrW / (y - 1);
                noiseFigureDbw[i] = WattsToDbw(noiseFigureWatts);
                // sum noise figure in watts for avg
                meanNoiseFigureDbw += noiseFigureWatts;

                // calculate gain
                double gainWatts = wattsNdOn /
                    (this.K * this.T0 * enbw * (enrW + noiseFigureWatts));
                gainDbw[i] = WattsToDbw(gainWatts);
                // sum gain for average
                meanGainDbw += gainWatts;

                // calculate mean power of receiver noise (Watts) 
                double meanPwrWatts = this.K * this.T0 *
                    enbw * noiseFigureWatts * gainWatts;
                meanPowerDbm[i] = WattsToDbm(meanPwrWatts * pkToPkAvg);
                meanPowerDbm[i] = meanPowerDbm[i] + cableLoss -
                    gainDbw[i] - antennaGain;
                meanDetectedSysNoise += DbmToWatts(meanPowerDbm[i]);
            }
            // finish taking averages and convert back to Logarithmic units
            meanGainDbw = WattsToDbw((meanGainDbw / GainDbw.Length));
            meanNoiseFigureDbw = 
                WattsToDbw((meanNoiseFigureDbw / noiseFigureDbw.Length));
            meanDetectedSysNoise =
                WattsToDbm((meanDetectedSysNoise / meanPowerDbm.Length));
        }

        // Calculates the peak-to-peak average ratio for spec analyzer
        // positive-peak-detected measurement of Gaussian noise
        private double PeakToPeakAvg(double dwellTime, double rbw)
        {
            return Math.Log(2 * Math.PI * dwellTime * 1.499 *
                rbw * Math.E);
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

        public double MeanGainDbw
        {
            get { return meanGainDbw; }
        }

        public double MeanNoiseFigureDbw
        {
            get { return meanNoiseFigureDbw; }
        }

        public double MeanDetectedSysNoise
        {
            get { return meanDetectedSysNoise; }
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