using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.ProcessingTools.FourierTransforms
{
    public class STFTCalculator : BaseCalculator
    {
        private FFTCalculator _fft;
        private int _windowSize;
        private int _hopSize;
        private int _numberOfWindows;
        private Complex[][] _frequencyDomainSignalSpectrum;
        private double[][] _magnitudeSpectrum;

        public STFTCalculator(double[] timeDomainSignal, int sampleRate, int windowSize, int hopSize)
        {
            _fft = new FFTCalculator(timeDomainSignal, sampleRate);
            _timeDomainSignal = timeDomainSignal;
            _sampleRate = sampleRate;
            _windowSize = windowSize;
            _hopSize = hopSize;
            _numberOfSamples = timeDomainSignal.Length;
            _numberOfWindows = (int)((timeDomainSignal.Length - windowSize) / hopSize) + 1;
        }

        public double[] ComputeTimeBinsInSecond(double[] timeDomainSignal)
        {
            double[] timeBins = new double[_numberOfWindows];
            for (int i = 0; i < _numberOfWindows; i++)
            {
                timeBins[i] = i * (double)_hopSize / _sampleRate;
            }
            return timeBins;
        }

        public Complex[][] ComputeFrequencyDomainSpectrum(double[] timeDomainSignal)
        {
            if (_frequencyDomainSignalSpectrum == null) ComputeSpectrum(timeDomainSignal);
            return _frequencyDomainSignalSpectrum;
        }

        public double[][] ComputeMagnitudeSpectrum(double[] timeDomainSignal)
        {
            if (_magnitudeSpectrum == null) ComputeSpectrum(timeDomainSignal);
            return _magnitudeSpectrum;
        }

        public (Complex[][], double[][]) ComputeSpectrum(double[] timeDomainSignal)
        {
            double[] windowFunction = HammingWindow(_windowSize);

            _frequencyDomainSignalSpectrum = new Complex[_numberOfWindows][];
            _magnitudeSpectrum = new double[_numberOfWindows][];
            //MagnitudeDBSpectrum = new float[numWindows][];

            for (int i = 0; i < _numberOfWindows; i++)
            {
                // Extract windowed segment
                double[] segment = new double[_windowSize];
                Array.Copy(timeDomainSignal, i * _hopSize, segment, 0, _windowSize);

                // Apply window function
                for (int j = 0; j < _windowSize; j++)
                {
                    segment[j] *= windowFunction[j];
                }

                // Compute FFT
                _frequencyDomainSignalSpectrum[i] = _fft.ComputeFrequencyDomainSignal(segment);
                _magnitudeSpectrum[i] = _fft.ComputeMagnitude(_frequencyDomainSignalSpectrum[i]);
                //MagnitudeDBSpectrum[i] = fft.MagnitudeDB;
            }
            return (_frequencyDomainSignalSpectrum, _magnitudeSpectrum);
        }

        private double[] HammingWindow(int size)
        {
            double[] window = new double[size];
            for (int i = 0; i < size; i++)
            {
                window[i] = 0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (size - 1));
            }
            return window;
        }

        public double[] ReconstructTimeDomainSignal(Complex[][] frequencyDomainSignalSpectrum)
        {
            double[] timeDomainSignalSpectrum = new double[_numberOfSamples];
            double[] windowFunction = HammingWindow(_windowSize);

            for (int i = 0; i < _numberOfWindows; i++)
            {
                Complex[] windowedFreqSignal = new Complex[_windowSize];
                Array.Copy(frequencyDomainSignalSpectrum[i], windowedFreqSignal, _windowSize);

                double[] windowedTimeSignal = _fft.ComputeInverseFFT(windowedFreqSignal);

                // Overlap-add the reconstructed signal
                for (int j = 0; j < _windowSize; j++)
                {
                    timeDomainSignalSpectrum[i * _hopSize + j] += windowedTimeSignal[j] * windowFunction[j];
                }
            }

            return timeDomainSignalSpectrum;
        }

        public (double[], Complex[][], double[][]) ReduceNoise(double[] noiseSignal, double frequencyBinInterval = 0, double threshold = 1e-10)
        {
            // Resize noise signal to match the length of the original signal
            double[] resizedNoiseSignal = ResizeSignal(noiseSignal, _numberOfSamples);

            // Step 1: Compute STFT for the noise signal
            var (noiseFreqDomainSpectrum, noiseMagnitudeSpectrum) = ComputeSpectrum(resizedNoiseSignal);

            // Step 2: Compute average noise magnitude spectrum
            double[] averageNoiseMagnitudeSpectrum = ComputeAverageMagnitude(noiseMagnitudeSpectrum);

            // Step 3: Compute STFT for the original signal (this will be done by accessing FrequencyDomainSignalSpectrum and MagnitudeSpectrum)
            var (cleanedFrequencyDomainSignalSpectrum, cleanedMagnitudeSpectrum) = ComputeSpectrum(_timeDomainSignal);

            // Step 4: Subtract the noise spectrum from the original spectrum
            for (int i = 0; i < _numberOfWindows; i++)
            {
                for (int j = 0; j < cleanedMagnitudeSpectrum[i].Length; j++)
                {
                    // Spectral subtraction
                    cleanedMagnitudeSpectrum[i][j] = Math.Max(0, cleanedMagnitudeSpectrum[i][j] - averageNoiseMagnitudeSpectrum[j]);

                    // Update the frequency domain signal with the cleaned magnitude and original phase
                    float phase = (float)Math.Atan2(cleanedFrequencyDomainSignalSpectrum[i][j].Imaginary, cleanedFrequencyDomainSignalSpectrum[i][j].Real);
                    cleanedFrequencyDomainSignalSpectrum[i][j] = Complex.FromPolarCoordinates(cleanedMagnitudeSpectrum[i][j], phase);
                }
            }

            // Step 5: Reconstruct the time-domain signal from the cleaned magnitude spectrum
            double[] cleanedTimeDomainSignalSpectrum = ReconstructTimeDomainSignal(cleanedFrequencyDomainSignalSpectrum);
            return (cleanedTimeDomainSignalSpectrum, cleanedFrequencyDomainSignalSpectrum, cleanedMagnitudeSpectrum);
        }

        // Method to compute the average magnitude spectrum
        public double[] ComputeAverageMagnitude(double[][] magnitudeSpectrum)
        {
            int length = magnitudeSpectrum[0].Length;
            double[] averageSpectrum = new double[length];

            for (int i = 0; i < magnitudeSpectrum.Length; i++)
            {
                for (int j = 0; j < length; j++)
                {
                    averageSpectrum[j] += magnitudeSpectrum[i][j];
                }
            }

            for (int j = 0; j < length; j++)
            {
                averageSpectrum[j] /= magnitudeSpectrum.Length;
            }

            return averageSpectrum;
        }

        public (double[][], double[]) FilterMagnitudeSpectrum(double[][] magnitudeSpectrum, double[] frequencyBins, double frequencyMin = 0, double frequencyMax = 5000)
        {
            // Find the indices of the frequency range
            int minIndex = Array.FindIndex(frequencyBins, f => f >= frequencyMin);
            int maxIndex = Array.FindLastIndex(frequencyBins, f => f <= frequencyMax);

            // Extract the relevant portion of the magnitude spectrum
            int numWindows = magnitudeSpectrum.Length;
            double[][] filteredMagnitudeSpectrum = new double[numWindows][];
            for (int i = 0; i < numWindows; i++)
            {
                filteredMagnitudeSpectrum[i] = magnitudeSpectrum[i].Skip(minIndex).Take(maxIndex - minIndex + 1).ToArray();
            }
            double[] filteredFrequencyBins = frequencyBins.Skip(minIndex).Take(maxIndex - minIndex + 1).ToArray();
            return (filteredMagnitudeSpectrum, filteredFrequencyBins);
        }
    }
}
