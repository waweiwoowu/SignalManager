using SignalManager.AudioTools.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using MathNet.Numerics.IntegralTransforms;

namespace SignalManager.ProcessingTools.FourierTransforms
{
    public class RFFTCalculator : FFTCalculator
    {
        public RFFTCalculator(double[] timeDomainSignal, int sampleRate)
            : base(timeDomainSignal, sampleRate) { }// Call the base class constructor

        public override double[] ComputeFrequencyBins(int sampleRate, int numberOfSamples)
        {
            double frequencyResolution = sampleRate / (2 * (numberOfSamples - 1));
            double[] frequencyBins = new double[numberOfSamples];

            // Calculate the frequency bins
            for (int i = 0; i < numberOfSamples; i++)
            {
                frequencyBins[i] = i * frequencyResolution;
            }

            return frequencyBins;
        }

        public override Complex[] ComputeFrequencyDomainSignal(double[] timeDomainSignal)
        {
            int numberOfSamples = timeDomainSignal.Length;
            // Adjust the size of the float array to accommodate the packed spectrum
            int packedSize = numberOfSamples % 2 == 0 ? numberOfSamples + 2 : numberOfSamples + 1;
            Array.Resize(ref timeDomainSignal, packedSize);

            // Initialize the frequency domain signal
            Complex[] frequencyDomainSignal = new Complex[numberOfSamples / 2 + 1];

            // Perform RFFT on the real array
            Fourier.ForwardReal(timeDomainSignal, numberOfSamples, FourierOptions.Matlab);

            // Copy the result to the Complex array
            for (int i = 0; i < frequencyDomainSignal.Length; i++)
            {
                frequencyDomainSignal[i] = new Complex(timeDomainSignal[2 * i], timeDomainSignal[2 * i + 1]);
            }

            return frequencyDomainSignal;
        }

        public (double[], Complex[]) ReduceNoise(double[] timeDomainSignal, double[] noiseSignal, int sampleRate, double frequencyBinInterval = 0, double threshold = 1e-10)
        {
            int numberOfSamples = timeDomainSignal.Length;

            // Resize noise signal to match the length of the input signal
            double[] resizedNoiseSignal = ResizeSignal(noiseSignal, numberOfSamples);
            Complex[] noiseFreqDomain = ComputeFrequencyDomainSignal(resizedNoiseSignal);

            // Compute the frequency domain signal
            if (_frequencyDomainSignal == null)
            {
                _frequencyDomainSignal = ComputeFrequencyDomainSignal(timeDomainSignal);
            }
            Complex[] cleanedFrequencyDomainSignal = _frequencyDomainSignal;
            Complex[] signal = new Complex[_frequencyDomainSignal.Length];

            // Subtract noise spectrum from the input signal's spectrum
            for (int i = 0; i < numberOfSamples; i++)
            {
                if (noiseFreqDomain[i].Magnitude >= cleanedFrequencyDomainSignal[i].Magnitude)
                {
                    cleanedFrequencyDomainSignal[i] = Complex.Zero;  // Consider setting to zero if noise is stronger
                }
                else
                {
                    double ratio = noiseFreqDomain[i].Magnitude / cleanedFrequencyDomainSignal[i].Magnitude;
                    cleanedFrequencyDomainSignal[i] *= (1 - ratio);
                }
                // Set the cleaned signal to zero if magnitude is very small (effectively zero)
                if (cleanedFrequencyDomainSignal[i].Magnitude < 1e-10)
                {
                    cleanedFrequencyDomainSignal[i] = Complex.Zero;
                }
                signal[i] = cleanedFrequencyDomainSignal[i];
            }

            // Prepare the real array for the inverse RFFT
            double[] cleanedSignal = new double[numberOfSamples + 2];
            for (int i = 0; i < cleanedFrequencyDomainSignal.Length; i++)
            {
                cleanedSignal[2 * i] = cleanedFrequencyDomainSignal[i].Real;
                cleanedSignal[2 * i + 1] = cleanedFrequencyDomainSignal[i].Imaginary;
            }

            // Perform the inverse RFFT
            Fourier.InverseReal(cleanedSignal, numberOfSamples, FourierOptions.Matlab);

            // Extract the real part of the signal
            double[] cleanedTimeDomainSignal = cleanedSignal.Take(numberOfSamples).ToArray();

            return (cleanedTimeDomainSignal, cleanedFrequencyDomainSignal);
        }
    }
}
