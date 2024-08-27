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
