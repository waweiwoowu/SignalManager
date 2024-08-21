using System;
using System.Linq;
using SignalManager.AudioTools.DataManager;

namespace SignalManager.AudioTools.ProcessingManager
{
    public partial class SignalProcessor
    {
        public void ReduceNoise()
        {
            IdentifyNoiseSegments();
            (OriginalSignalData.TimeDomainSignal, OriginalSignalData.FrequencyDomainSignalSpectrum, OriginalSignalData.MagnitudeSpectrum) = OriginalSignalData.STFT.ReduceNoise(NoiseSignalData.TimeDomainSignal);
        }

        public void IdentifyNoiseSegments()
        {
            double[] potentialNoiseSignal = new double[OriginalSignalData.TimeDomainSignal.Length];
            int noiseSegmentLength = 0;
            int pulseEndIndex = 0;
            int noiseDropEndIndex = 0;
            bool isWithinBackgroundNoise = true;
            bool isWithinDroppingNoise = false;

            // First pass: Identify and count the noise segments
            for (int sampleIndex = 0; sampleIndex < OriginalSignalData.TimeDomainSignal.Length; sampleIndex++)
            {
                if (isWithinBackgroundNoise)
                {
                    if (OriginalSignalData.NoiseDropSampleIndices.Contains(sampleIndex))
                    {
                        noiseDropEndIndex = sampleIndex + OriginalSignalData.WindowSize;
                        isWithinDroppingNoise = true;
                    }

                    if (!isWithinDroppingNoise)
                    {
                        potentialNoiseSignal[noiseSegmentLength] = OriginalSignalData.TimeDomainSignal[sampleIndex]; // Use noiseSize as the index
                        noiseSegmentLength++; // Increment only if it's background (noise)
                    }

                    if (sampleIndex == noiseDropEndIndex)
                    {
                        isWithinDroppingNoise = false;
                    }
                }

                if (OriginalSignalData.PulseSampleIndices.Contains(sampleIndex))
                {
                    pulseEndIndex = sampleIndex + OriginalSignalData.PulseWidth;
                    isWithinBackgroundNoise = false;
                }

                if (sampleIndex == pulseEndIndex)
                {
                    isWithinBackgroundNoise = true;
                }
            }

            // Create a resized array for the noise-only signal
            double[] noiseOnlySignal = new double[noiseSegmentLength];
            Array.Copy(potentialNoiseSignal, noiseOnlySignal, noiseSegmentLength);

            // Assign the resized signal and other properties to NoiseData
            NoiseSignalData.TimeDomainSignal = noiseOnlySignal;
        }
    }
}
