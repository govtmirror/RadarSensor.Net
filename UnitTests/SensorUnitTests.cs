using System;
using AgilentN6841A;
using General;
using SensorFrontEnd;
using System.IO;
using System.Web;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RadarSensorTests
{
    [TestClass]
    public class SensorUnitTests
    {
        [TestMethod]
        public void DbmToWattsTest()
        {
            double[] dbmnInputVals = { 10, 20, 30, 50, 60 };
            double[] expectedWattValues = { 0.01,  0.1, 1, 100, 1000 };
            
            for (int i = 0; i < dbmnInputVals.Length; i++)
            {
                Assert.AreEqual(Yfactor.DbmToWatts(dbmnInputVals[i]),
                    expectedWattValues[i]);
            }

            double dbmInputVal = 25.0;
            double dbmExpectedVal = 0.316227766;
            double tolerance = .00001;
            Assert.IsTrue(Math.Abs(Yfactor.DbmToWatts(dbmInputVal) - 
                dbmExpectedVal) <= tolerance);       
        }

        [TestMethod]
        public void WattsToDbmTest()
        {
            double[] wattsInputVals = { 0.01,  0.1, 1, 100, 1000 };
            double[] expectedDbmValues = { 10, 20, 30, 50, 60 };

            for (int i = 0; i < wattsInputVals.Length; i++)
            {
                Assert.AreEqual(Yfactor.WattsToDbm(wattsInputVals[i]),
                    expectedDbmValues[i]);
            }
        }

        [TestMethod]
        public void WattsToDbwTest()
        {
            double TOLERANCE = 0.01;
            double wattInput = 15.0;
            double expectedValue = 11.7609;

            Assert.IsTrue(Math.Abs(Yfactor.WattsToDbw(wattInput) -
                expectedValue) <= TOLERANCE);
        }

        [TestMethod]
        public void DbwToWattsTest()
        {
            double TOLERANCE = 0.01;
            double dbwInput = 14.46;
            double expectedValue = 27.9254;
            Assert.IsTrue(Math.Abs(Yfactor.DbwToWatts(dbwInput) -
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

            SensorDriver sensor = new SensorDriver();
            Preselector preselector = 
                new Preselector(Constants.PRESELECTOR_IP);

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

            GetListOfFreqs(centerFrequencies, 
                Constants.CenterFrequencyValues);
            GetListOfFreqs(frequencies, Constants.FrequeencyValues);

            Assert.IsTrue(centerFrequencies.SequenceEqual(
                fftParams.CenterFrequencies));
            Assert.IsTrue(frequencies.SequenceEqual(
                fftParams.FrequencyList));
        }

        [TestMethod]
        public void YfactorCalTest()
        {
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
            
            double TOLERANCE = 0.1;

            Yfactor yFactor = new Yfactor(noiseDiodeOnInputs,
                noiseDiodeOffInputs, rbw, enbw, dwellTime, enr, 
                cableLoss, antennaGain);

            Assert.IsTrue(Math.Abs(yFactor.MeanNoiseFigureDbw - 
                expectedNoiseFigure) <= TOLERANCE);

            Assert.IsTrue(Math.Abs(yFactor.MeanGainDbw - 
                expectedGain) <= TOLERANCE);
        }

        // reads csv file into List<double>
        private void GetListOfFreqs(List<double> frequencies, 
            string path) 
        {
            var reader = new StreamReader(File.OpenRead(path));
            while (!reader.EndOfStream)
            {
                string line = reader.ReadLine();
                string[] values = line.Split(',');
                for (int i = 0; i < values.Length; i++)
                {
                    frequencies.Add(Double.Parse(values[i]));
                }
            }
        }
    }
}
