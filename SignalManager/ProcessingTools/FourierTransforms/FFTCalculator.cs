using SignalManager.AudioTools.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace SignalManager.ProcessingTools.FourierTransforms
{
    public class FFTCalculator : BaseCalculator
    {
        protected Complex[] _frequencyDomainSignal;

        public FFTCalculator(double[] timeDomainSignal, int sampleRate)
        {
            _timeDomainSignal = timeDomainSignal;
            _sampleRate = sampleRate;
            _numberOfSamples = timeDomainSignal.Length;
        }

        public virtual Complex[] ComputeFrequencyDomainSignal(double[] timeDomainSignal)
        {
            int numberOfSamples = timeDomainSignal.Length;
            _frequencyDomainSignal = new Complex[numberOfSamples];

            // Convert time domain signal to complex array
            for (int i = 0; i < numberOfSamples; i++)
            {
                _frequencyDomainSignal[i] = new Complex(timeDomainSignal[i], 0.0);
            }

            // Perform FFT on the complex array
            Fourier.Forward(_frequencyDomainSignal, FourierOptions.Matlab);

            return _frequencyDomainSignal;
        }

        public double[] ComputeInverseFFT(Complex[] frequencyDomainSignal)
        {
            Fourier.Inverse(frequencyDomainSignal, FourierOptions.Matlab);
            return frequencyDomainSignal.Select(c => c.Real).ToArray();
        }

        public double[] ComputeMagnitudeInDb(double[] magnitude)
        {
            return magnitude.Select(m => 20 * Math.Log10(m)).ToArray();
        }

        public virtual (double[] cleanedTimeDomainSignal, Complex[] cleanedFrequencyDomainSignal) ReduceNoise(double[] noiseSignal, double frequencyBinInterval = 0, double threshold = 1e-10)
        {
            double[] resizedNoiseSignal = ResizeSignal(noiseSignal, _numberOfSamples);
            Complex[] noiseFreqDomain = ComputeFrequencyDomainSignal(resizedNoiseSignal);

            if (_frequencyDomainSignal == null)
            {
                _frequencyDomainSignal = ComputeFrequencyDomainSignal(_timeDomainSignal);
            }

            Complex[] cleanedFrequencyDomainSignal = (Complex[])_frequencyDomainSignal.Clone();
            int binWidth = ComputeBinWidth(frequencyBinInterval);

            // Subtract noise spectrum using spectral subtraction
            for (int i = 0; i < _numberOfSamples; i += binWidth)
            {
                ApplySpectralSubtraction(cleanedFrequencyDomainSignal, noiseFreqDomain, i, binWidth, threshold);
            }

            double[] cleanedTimeDomainSignal = ComputeInverseFFT(cleanedFrequencyDomainSignal);
            return (cleanedTimeDomainSignal, cleanedFrequencyDomainSignal);
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

        private int ComputeBinWidth(double frequencyBinInterval)
        {
            if (frequencyBinInterval > 0)
            {
                double frequencyResolution = (double)_sampleRate / _numberOfSamples;
                return (int)(frequencyBinInterval / frequencyResolution);
            }
            return 1;
        }

        private void ApplySpectralSubtraction(Complex[] cleanedSignal, Complex[] noiseSignal, int startIndex, int binWidth, double threshold)
        {
            double signalMagnitude = 0;
            double noiseMagnitude = 0;

            for (int j = startIndex; j < startIndex + binWidth && j < _numberOfSamples; j++)
            {
                signalMagnitude += cleanedSignal[j].Magnitude;
                noiseMagnitude += noiseSignal[j].Magnitude;
            }

            for (int j = startIndex; j < startIndex + binWidth && j < _numberOfSamples; j++)
            {
                if (noiseMagnitude >= signalMagnitude)
                {
                    cleanedSignal[j] = Complex.Zero;
                }
                else
                {
                    double ratio = noiseMagnitude / signalMagnitude;
                    cleanedSignal[j] *= (1 - ratio);
                }

                if (cleanedSignal[j].Magnitude < threshold)
                {
                    cleanedSignal[j] = Complex.Zero;
                }
            }
        }
    }
}
