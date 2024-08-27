using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.ProcessingTools.FourierTransforms
{
    public class BaseCalculator
    {
        protected int _sampleRate;
        protected int _numberOfSamples;
        protected double[] _timeDomainSignal;

        public double[] ComputeTimeBins(double[] timeDomainSignal, int sampleRate)
        {
            return timeDomainSignal.Select((_, index) => (double)index / sampleRate).ToArray();
        }

        public virtual double[] ComputeFrequencyBins()
        {
            double frequencyResolution = (double)_sampleRate / _numberOfSamples;
            double[] frequencyBins = new double[_numberOfSamples / 2 + 1];

            // Calculate the frequency bins
            for (int i = 0; i < frequencyBins.Length; i++)
            {
                frequencyBins[i] = i * frequencyResolution;
            }

            return frequencyBins;
        }

        public virtual double[] GetFrequencyBinsWithInterval(double frequencyBinInterval)
        {
            double frequencyResolution = (double)_sampleRate / _numberOfSamples;
            int binWidth = (int)(frequencyBinInterval / frequencyResolution);

            // Calculate the number of new bins
            int newSize = _numberOfSamples / binWidth + 1;
            double[] newFrequencyBins = new double[newSize];

            // Populate the new frequency bins
            for (int i = 0; i < newSize; i++)
            {
                newFrequencyBins[i] = i * frequencyBinInterval;
            }

            return newFrequencyBins;
        }


        public double[] ComputeAmplitude(double[] timeDomainSignal)
        {
            return timeDomainSignal.Select(c => Math.Abs(c)).ToArray();
        }

        public double[] ComputeMagnitude(Complex[] frequencyDomainSignal)
        {
            return frequencyDomainSignal.Select(c => c.Magnitude).ToArray();
        }

        public double[] ComputeMagnitudeInDb(double[] magnitude)
        {
            return magnitude.Select(m => 20 * Math.Log10(m)).ToArray();
        }

        public double[] ResizeSignal(double[] signal, int length)
        {
            if (signal.Length < length)
            {
                int copyCount = length / signal.Length;
                int remaining = length % signal.Length;
                double[] resizedSignal = new double[length];
                for (int i = 0; i < copyCount; i++)
                {
                    Array.Copy(signal, 0, resizedSignal, i * signal.Length, signal.Length);
                }
                if (remaining > 0)
                {
                    Array.Copy(signal, 0, resizedSignal, copyCount * signal.Length, remaining);
                }
                return resizedSignal;
            }
            else if (signal.Length > length)
            {
                return signal.Take(length).ToArray();  // Trim the signal
            }
            return signal;
        }

        public virtual (double[] newFrequencyBins, double[] combinedMagnitude) ComputeMagnitudeInFrequencyBinInterval(double[] magnitude, int sampleRate, int numberOfSamples, double frequencyBinInterval)
        {
            double frequencyResolution = (double)sampleRate / numberOfSamples;
            int binWidth = (int)(frequencyBinInterval / frequencyResolution);
            int newSize = numberOfSamples / binWidth + 1;

            double[] combinedMagnitude = new double[newSize];
            double[] newFrequencyBins = new double[newSize];

            // Combine frequencies and calculate new bins
            for (int i = 0; i < numberOfSamples; i += binWidth)
            {
                double combinedValue = 0;
                int binCount = 0;

                for (int j = i; j < i + binWidth && j < numberOfSamples; j++)
                {
                    combinedValue += magnitude[j];
                    binCount++;
                }

                combinedMagnitude[i / binWidth] = combinedValue / binCount;
                newFrequencyBins[i / binWidth] = i * frequencyResolution;
            }

            return (newFrequencyBins, combinedMagnitude);
        }

        public (double[] frequencies, double[] magnitudes) FindCharacteristicFrequencies(double frequencyBinInterval, double[] magnitude, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            var (combinedFrequencyBins, combinedMagnitudes) = ComputeMagnitudeInFrequencyBinInterval(magnitude, _sampleRate, _numberOfSamples, frequencyBinInterval);
            return FindCharacteristicFrequencies(combinedFrequencyBins, combinedMagnitudes, peakFrequencyCount, minFrequency, maxFrequency);
        }

        public (double[] frequencies, double[] magnitudes) FindCharacteristicFrequencies(double[] combinedFrequencyBins, double[] combinedMagnitudes, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            var frequencyMagnitudePairs = combinedFrequencyBins
                .Select((freq, idx) => new { freq, magnitude = combinedMagnitudes[idx] })
                .Where(pair => pair.freq != 0 && pair.freq >= minFrequency && pair.freq <= maxFrequency)
                .OrderByDescending(pair => pair.magnitude)
                .Take(peakFrequencyCount)
                .ToArray();

            return (frequencyMagnitudePairs.Select(pair => pair.freq).ToArray(),
                    frequencyMagnitudePairs.Select(pair => pair.magnitude).ToArray());
        }
    }
}
