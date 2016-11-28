using System;
using AgilentN6841A;
using General;
using System.IO;
using System.Web.Script.Serialization;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RadarSensorTests
{
    [TestClass]
    public class SensorUnitTests
    {
        Config config = new JavaScriptSerializer().
            Deserialize<Config>(File.ReadAllText(Constants.ConfigFile));

        [TestMethod]
        public void DbmToWattsTest()
        {
            double[] dbmnInputVals = { 10, 20, 30, 50, 60 };
            double[] expectedWattValues = { 0.01,  0.1, 1, 100, 1000 };
            
            for (int i = 0; i < dbmnInputVals.Length; i++)
            {
                Assert.AreEqual(YfactorCal.DbmToWatts(dbmnInputVals[i]),
                    expectedWattValues[i]);
            }

            double dbmInputVal = 25.0;
            double dbmExpectedVal = 0.316227766;
            double tolerance = .00001;
            Assert.IsTrue(Math.Abs(YfactorCal.DbmToWatts(dbmInputVal) - 
                dbmExpectedVal) <= tolerance);       
        }

        [TestMethod]
        public void WattsToDbmTest()
        {
            double[] wattsInputVals = { 0.01,  0.1, 1, 100, 1000 };
            double[] expectedDbmValues = { 10, 20, 30, 50, 60 };

            for (int i = 0; i < wattsInputVals.Length; i++)
            {
                Assert.AreEqual(YfactorCal.WattsToDbm(wattsInputVals[i]),
                    expectedDbmValues[i]);
            }
        }

        [TestMethod]
        public void WattsToDbwTest()
        {
            double TOLERANCE = 0.01;
            double wattInput = 15.0;
            double expectedValue = 11.7609;

            Assert.IsTrue(Math.Abs(YfactorCal.WattsToDbw(wattInput) -
                expectedValue) <= TOLERANCE);
        }

        [TestMethod]
        public void DbwToWattsTest()
        {
            double TOLERANCE = 0.01;
            double dbwInput = 14.46;
            double expectedValue = 27.9254;
            Assert.IsTrue(Math.Abs(YfactorCal.DbwToWatts(dbwInput) -
                expectedValue) <= TOLERANCE);
        }

        [TestMethod]
        public void FftParamsTests()
        {
            // expected values for inputs 
            uint numFftBins = 64;
            uint numValidFftBins = 44;
            uint numValidFftBinsLastSeg = 18;
            uint numFfts = 43750;

            List<double> centerFrequencies = new List<double>();    
            List<double> frequencies = new List<double>();

            SensorDriver sensor = 
                new SensorDriver(config.PreselectorIp, 
                config.SensorHostName);

            SweepParams measParams;
            string json = File.ReadAllText(Constants.Spn43CalSweepParamsFile);
            measParams =
                new System.Web.Script.Serialization.
                JavaScriptSerializer().Deserialize<SweepParams>(
                    json);

            FFTParams fftParams 
                = new FFTParams(sensor.SensorCapabilities, 
                    measParams, sensor.PossibleSampleRates,
                    sensor.PossibleSpans);

            Assert.IsTrue(fftParams.NumFftBins == numFftBins);
            Assert.IsTrue(fftParams.NumValidFftBins == numValidFftBins);
            Assert.IsTrue(fftParams.NumBinsLastSegment ==
                numValidFftBinsLastSeg);

            GetListOfValuesFromCsv(centerFrequencies, 
                Constants.CenterFrequencyValues);
            GetListOfValuesFromCsv(frequencies, Constants.FrequeencyValues);

            Assert.IsTrue(centerFrequencies.SequenceEqual(
                fftParams.CenterFrequencies));
            Assert.IsTrue(frequencies.SequenceEqual(
                fftParams.FrequencyList));
        }

        [TestMethod]
        public void YfactorCalTest()
        {
            // Test 1 -----------------------------------------
            List<double> noiseDiodeOffInputs = new List<double>
            { 
                -89.77223497, -89.59538809, -89.81342543, -89.63387354, 
                -89.57563198, -90.31659269, -90.25265354, -89.85487336, 
                -89.56254714, -89.66427483 
            };

            List<double> noiseDiodeOnInputs = new List<double>
            { 
                -83.57892449, -83.40207761, -83.62011495, -83.44056306, 
                -83.38232149, -84.12328221, -84.05934306, -83.66156288,
                -83.36923666, -83.47096435 
            };

            double enr = 10.0; // db
            double enbw = 1e6; // Hz 
            double cableLoss = 1.0;
            double antennaGain = 1.0;
            double rbw = 1e3;
            double dwellTime = 0.0;

            // mean values of gain and noise figure arrays 
            // from noiseDiode on and off inputs
            double expectedGain = 19.1730796;
            double expectedNoiseFigure = 5.0;
            
            double TOLERANCE = 0.2;

            YfactorCal yFactor = new YfactorCal(noiseDiodeOnInputs,
                noiseDiodeOffInputs, rbw, enbw, dwellTime, enr, 
                cableLoss, antennaGain);

            Assert.IsTrue(Math.Abs(yFactor.MeanNoiseFigureDbw - 
                expectedNoiseFigure) <= TOLERANCE);

            Assert.IsTrue(Math.Abs(yFactor.MeanGainDbw - 
                expectedGain) <= TOLERANCE);

            // Test 2 --------------------------------------------
            List<double> noiseDiodeOffInputs2 = new List<double>();
            List<double> noiseDiodeOnInputs2 = new List<double>();

            GetListOfValuesFromCsv(noiseDiodeOffInputs2,
                Constants.NoiseDiodeOffInputs);
            GetListOfValuesFromCsv(noiseDiodeOnInputs2,
                Constants.NoiseDiodeOnInputs);
            double rbw2 = 437500;
            double enbw2 = 962500;
            double dwellTime2 = 0.1;
            double enr2 = 14.46;
            double gain2 = 1.5;
            double cableLoss2 = 0.8;
            double antennaGain2 = -1;

            YfactorCal yFactor2 = new YfactorCal(noiseDiodeOnInputs2,
                noiseDiodeOffInputs2, rbw2, enbw2, dwellTime2,
                enr2, cableLoss2, antennaGain2);

            List<double> expectedNoiseFigure2 = new List<double>();
            List<double> expectedGain2 = new List<double>();

            GetListOfValuesFromCsv(expectedNoiseFigure2,
                Constants.ExpectedNoiseFigure);
            GetListOfValuesFromCsv(expectedGain2,
                Constants.ExcpectedGain);

            double meanExpectedNoiseFigure2 = 0;
            double meanExpectedGain2 = 0;

            for (int i = 0; i < expectedGain2.Count; i++)
            {
                meanExpectedGain2 += expectedGain2[i];
                meanExpectedNoiseFigure2 += expectedNoiseFigure2[i];
            }
            meanExpectedNoiseFigure2 /= expectedNoiseFigure2.Count;
            meanExpectedGain2 /= expectedGain2.Count;

            Assert.IsTrue(Math.Abs(yFactor2.MeanGainDbw -
                meanExpectedGain2) <= TOLERANCE);
            Assert.IsTrue(Math.Abs(yFactor2.MeanNoiseFigureDbw -
                meanExpectedNoiseFigure2) <= TOLERANCE);
        }

        // reads csv file into List<double>
        private void GetListOfValuesFromCsv(List<double> vals, 
            string path) 
        {
            var reader = new StreamReader(File.OpenRead(path));
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(',');
                for (int i = 0; i < values.Length; i++)
                {
                    vals.Add(Double.Parse(values[i]));
                }
            }
        }
    }
}
