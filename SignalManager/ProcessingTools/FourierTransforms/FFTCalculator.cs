using SignalManager.AudioTools.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;
//using NAudio.Dsp;

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

            // Convert the time domain signal to a complex array
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
            double[] timeDomainSignal = new double[frequencyDomainSignal.Length];
            Fourier.Inverse(frequencyDomainSignal, FourierOptions.Matlab);
            timeDomainSignal = frequencyDomainSignal.Select(c => c.Real).ToArray();
            return timeDomainSignal;
        }

        public double[] ComputeMagnitudeInDb(double[] magnitude)
        {
            double[] magnitudeDb = new double[magnitude.Length];
            for (int i = 0; i < magnitude.Length; i++)
            {
                magnitudeDb[i] = 20 * Math.Log10(magnitude[i]);
            }
            return magnitudeDb;
        }

        public virtual (double[], Complex[]) ReduceNoise(double[] noiseSignal, double frequencyBinInterval = 0, double threshold = 1e-10)
        {
            // Resize noise signal to match the length of the input signal
            double[] resizedNoiseSignal = ResizeSignal(noiseSignal, _numberOfSamples);
            Complex[] noiseFreqDomain = ComputeFrequencyDomainSignal(resizedNoiseSignal);

            // Compute the frequency domain signal
            if (_frequencyDomainSignal == null)
            {
                _frequencyDomainSignal = ComputeFrequencyDomainSignal(_timeDomainSignal);
            }
            Complex[] cleanedFrequencyDomainSignal = _frequencyDomainSignal;
            Complex[] signal = new Complex[_frequencyDomainSignal.Length];

            int binWidth = 1;
            if (frequencyBinInterval > 0)
            {
                double frequencyResolution = (double)_sampleRate / _numberOfSamples;
                binWidth = (int)(frequencyBinInterval / frequencyResolution);
            }

            // Subtract noise spectrum from the input signal's spectrum using spectral subtraction
            for (int i = 0; i < _numberOfSamples; i += binWidth)
            {
                double signalMagnitude = 0;
                double noiseMagnitude = 0;
                for (int j = i; j < i + binWidth && j < _numberOfSamples; j++)
                {
                    signalMagnitude += cleanedFrequencyDomainSignal[j].Magnitude;
                    noiseMagnitude += noiseFreqDomain[j].Magnitude;
                }

                for (int j = i; j < i + binWidth && j < _numberOfSamples; j++)
                {
                    if (noiseMagnitude >= signalMagnitude)
                    {
                        cleanedFrequencyDomainSignal[j] = Complex.Zero;  // Set to zero if noise is stronger
                    }
                    else
                    {
                        double ratio = noiseMagnitude / signalMagnitude;
                        cleanedFrequencyDomainSignal[j] *= (1 - ratio);
                    }

                    // Apply thresholding
                    if (cleanedFrequencyDomainSignal[j].Magnitude < threshold)
                    {
                        cleanedFrequencyDomainSignal[j] = Complex.Zero;
                    }

                    signal[j] = cleanedFrequencyDomainSignal[j];
                }
            }

            // Perform inverse FFT to get the cleaned signal
            Fourier.Inverse(signal, FourierOptions.Matlab);
            double[] cleanedTimeDomainSignal = signal.Select(c => c.Real).ToArray();

            return (cleanedTimeDomainSignal, cleanedFrequencyDomainSignal);
        }

        public virtual (double[] newFrequencyBins, double[] combinedMagnitude) ComputeMagnitudeInFrequencyBinInterval(double[] magnitude, int sampleRate, int numberOfSamples, double frequencyBinInterval)
        {
            double frequencyResolution = (double)sampleRate / numberOfSamples;
            int binWidth = (int)(frequencyBinInterval / frequencyResolution);

            // Calculate the size of the new arrays
            int newSize = numberOfSamples / binWidth + 1;
            double[] combinedMagnitude = new double[newSize];
            double[] newFrequencyBins = new double[newSize];

            // Combine frequencies within the target range and calculate new frequency bins
            for (int i = 0; i < numberOfSamples; i += binWidth)
            {
                double combinedValue = 0;
                int binCount = 0;

                for (int j = i; j < i + binWidth && j < numberOfSamples; j++)
                {
                    combinedValue += magnitude[j];
                    binCount++;
                }

                combinedMagnitude[i / binWidth] = combinedValue / binCount; // Averaging
                newFrequencyBins[i / binWidth] = i * frequencyResolution; // New frequency bin
            }

            return (newFrequencyBins, combinedMagnitude);
        }

        public (double[] frequencies, double[] magnitudes) FindCharacteristicFrequencies(double frequencyBinInterval, double[] magnitude, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            (double[] combinedFrequencyBins, double[] combinedMagnitudes) = ComputeMagnitudeInFrequencyBinInterval(magnitude, _sampleRate, _numberOfSamples, frequencyBinInterval);

            var (characteristicFrequencies, characteristicMagnitudes) = FindCharacteristicFrequencies(combinedFrequencyBins, combinedMagnitudes, peakFrequencyCount, minFrequency, maxFrequency);

            return (characteristicFrequencies, characteristicMagnitudes);
        }

        public (double[] frequencies, double[] magnitudes) FindCharacteristicFrequencies(double[] combinedFrequencyBins, double[] combinedMagnitudes, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            // Filter frequencies within the specified range
            var frequencyMagnitudePairs = new List<(double frequency, double magnitude)>();
            for (int i = 0; i < combinedFrequencyBins.Length; i++)
            {
                var debug = combinedFrequencyBins[i];
                if (combinedFrequencyBins[i] != 0 && combinedFrequencyBins[i] >= minFrequency && combinedFrequencyBins[i] <= maxFrequency)
                {
                    frequencyMagnitudePairs.Add((combinedFrequencyBins[i], combinedMagnitudes[i]));
                }
            }

            // Sort the list by magnitude in descending order
            var sortedPairs = frequencyMagnitudePairs.OrderByDescending(pair => pair.magnitude).Take(peakFrequencyCount).ToList();

            // Extract the frequencies and magnitudes of the characteristic frequencies
            double[] characteristicFrequencies = sortedPairs.Select(pair => pair.frequency).ToArray();
            double[] characteristicMagnitudes = sortedPairs.Select(pair => pair.magnitude).ToArray();

            return (characteristicFrequencies, characteristicMagnitudes);
        }
    }
}
