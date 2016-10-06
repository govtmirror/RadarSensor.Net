using AgSal;
using System;
using System.Linq;
using System.Collections.Generic;

namespace AgilentN6841A
{
    public class FFTParams
    {
        #region fields
        private MeasurmentParams measParams;
        // center frequency (Hz) for the ffts that comprise of the span
        private List<double> centerFrequencies 
            = new List<double>();
        // sample rate (samples per second
        private double sampleRate;
        // number of fft bins
        private int numFftBins;
        // List of frequencies (Hz)
        private List<double> frequencyList 
            = new List<double>();
        // number of valid bins in full segment (equals N if rmvAA=0)
        private int numValidFftBins;
        // number of bins in last segment 
        private int numBinsLastSegment;
        // detector (RMS, Positive, Sample)
        private string detector;
        // number of FFTs for detector (if detector = sample numFFts = 1)
        private int numFfts;
        // window: 'Hanning', 'Gauss-top', 'Flattop', 'Rectangular'
        
        // Agilent sensor capabilites struct
        private AgSalLib.SensorCapabilities sensorCapabilities;

        // initialize array of powers of 2 for 
        // possible number of FFT bins in segment
        private int[] possibleFftBins = { 8, 16, 32, 64, 128, 256, 512,
            1024,  2048, 4096, 8192, 16348 };

        private Dictionary<string, double> windows = new Dictionary<string, double>()
        {
            { "Rectangular", 1.0 },
            { "Flattop", 3.8 },
            { "Gauss-top", 2.2 },
            { "Hanning", 1.5 },
            { "Blackman-Harris", 2.0 }
        };
        #endregion

        public FFTParams() { }

        public FFTParams(AgSalLib.SensorCapabilities c,
            MeasurmentParams m)
        {
            sensorCapabilities = c;
            measParams = m;
        }

        #region Properties
        public List<double> CenterFrequencies
        {
            get { return centerFrequencies; }
        }

        public double SampleRate
        {
            get { return sampleRate; }
        }

        public int NumFftBins
        {
            get { return numFftBins; }
        }

        public List<double> FrequencyList
        {
           get { return FrequencyList; }
        }

        public int NumValidFftBins
        {
            get { return NumValidFftBins; }
        }

        public int NumBinsLastSegment
        {
            get { return NumBinsLastSegment; }
        }

        public int NumFfts
        {
            get { return numFftBins; }
        }
        #endregion

        public void calcFftParameters(double[] possibleSampleRates, 
            double[] possibleSpans)
        {
            // calculate possible sample rates
            double span = measParams.Fstop - measParams.Fstart;
            Console.WriteLine("span: " + span + "\n");
            if (span >= sensorCapabilities.maxSpan)
            {
                sampleRate = possibleSampleRates.Max();
            }
            else
            {
                // find last element that is greather than the span
                // list is already sorted in decending order
                for (int i = 1; i < possibleSpans.Length; i++)
                {
                    if (possibleSpans[i] < span)
                    {
                        sampleRate = possibleSampleRates[i - 1];
                        Console.WriteLine("sampleRate: " + sampleRate);
                        break;
                    }
                    else if (i == possibleSpans.Length - 1)
                    {
                        sampleRate =
                            possibleSampleRates[possibleSampleRates.Length - 1];
                    }
                }
            }
            Console.WriteLine("sampleRate: " + sampleRate);

            // calculate num of FFT binsdouble winValue;
            double winValue;
            windows.TryGetValue(measParams.Window, out winValue);
            for (int i = 1; i < possibleFftBins.Length; i++)
            {
                double possibleEnbw = (winValue * sampleRate) /
                    possibleFftBins[i];
                if (possibleEnbw <= measParams.Bw)
                {
                    numFftBins = possibleFftBins[i];
                    Console.WriteLine("num fft bins: " + numFftBins);
                    break;
                }
                else if (i == possibleFftBins.Length - 1)
                {
                    Console.WriteLine("num fft bins: " + numFftBins);
                    numFftBins =
                        possibleFftBins[possibleFftBins.Length - 1];
                }
            }
            // determine number of valid fft bins not affected by 
            // anti-aliasing filter
            double validSpanRaito;
            if (sampleRate == sensorCapabilities.maxSampleRate)
            {
                validSpanRaito = sensorCapabilities.maxSampleRate /
                    sensorCapabilities.maxSpan;
            }
            else
            {
                validSpanRaito = sensorCapabilities.sampleRateToSpanRatio;
            }

            numValidFftBins = floorEven(numFftBins / validSpanRaito);
            int idx1; // index of first valid
            idx1 = (numFftBins - numValidFftBins) / 2 + 1;

            // calculate number of segments
            double binResolution = sampleRate / numFftBins;
            double segmentSpan = binResolution * numValidFftBins;
            double numFullSegments = Math.Floor(span / segmentSpan);

            double nextCenterFrequency;  // center freq for next segment
            nextCenterFrequency = measParams.Fstart + binResolution / 2
                + binResolution * (numFftBins / 2 - idx1)
                + binResolution * numValidFftBins * numFullSegments;

            int numSegments;
            if (nextCenterFrequency > sensorCapabilities.maxFrequency)
            {
                numSegments = (int) numFullSegments;
                numBinsLastSegment = numValidFftBins;
            }
            else
            {
                numSegments = (int)numFullSegments + 1;
                numBinsLastSegment = (int)Math.Ceiling((span % segmentSpan)
                    * numValidFftBins / segmentSpan);
            }

            // calculate center frequencies 
            for (int i = 0; i < numSegments; i++)
            {
                centerFrequencies.Add(measParams.Fstart + (binResolution / 2)
                    + (binResolution * (numFftBins / 2 - idx1))
                    + (binResolution * numValidFftBins * i));
            }

            // calculate frequencies for the span 
            double numFrequencies = (numValidFftBins * numFullSegments +
                numBinsLastSegment * (numSegments - numFullSegments) - 1);

            for (int i = 0; i < numFrequencies; i++)
            {
                frequencyList.Add(measParams.Fstart + 
                    (binResolution / 2) * binResolution * i);
            }

            // specify number of FFTs to detect over
            if (measParams.TimeOverlap == 0)
            {
                numFfts = (int)Math.Ceiling(measParams.DwellTime * sampleRate
                    / numFftBins);
            } 
            else if (measParams.TimeOverlap == 50)
            {
                numFfts = (int)Math.Ceiling(2 * measParams.DwellTime 
                    * sampleRate / numFftBins);
            }
        }

        // round towards nearest even integer 
        private int floorEven(double num)
        {
            int val = (int)Math.Floor(num);
            if (val  % 2 == 0)
            {
                return val;
            }
            else
            {
                return val - 1;
            }
        }
    }
}
