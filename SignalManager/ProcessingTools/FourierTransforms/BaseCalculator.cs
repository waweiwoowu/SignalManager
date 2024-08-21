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

        public virtual double[] ComputeFrequencyBins(int sampleRate, int numberOfSamples)
        {
            double frequencyResolution = (double)sampleRate / numberOfSamples;
            double[] frequencyBins = new double[numberOfSamples / 2 + 1];

            // Calculate the frequency bins
            for (int i = 0; i < frequencyBins.Length; i++)
            {
                frequencyBins[i] = i * frequencyResolution;
            }

            return frequencyBins;
        }

        public double[] ComputeAmplitude(double[] timeDomainSignal)
        {
            return timeDomainSignal.Select(c => Math.Abs(c)).ToArray();
        }

        public double[] ComputeMagnitude(Complex[] frequencyDomainSignal)
        {
            return frequencyDomainSignal.Select(c => c.Magnitude).ToArray();
        }

        public double[] MagnitudeToDb(double[] magnitude)
        {
            double[] magnitudeDb = new double[magnitude.Length];
            for (int i = 0; i < magnitude.Length; i++)
            {
                magnitudeDb[i] = 20 * Math.Log10(magnitude[i]);
            }
            return magnitudeDb;
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
    }
}
