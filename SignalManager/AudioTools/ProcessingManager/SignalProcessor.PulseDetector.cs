using System;
using System.Collections.Generic;
using System.Linq;

namespace SignalManager.AudioTools.ProcessingManager
{
    public partial class SignalProcessor
    {
        private int _totalWindows;

        public void DetectPulses(int sampleOffset = -44100, double thresholdMultiplier = 0.8, int minimumPulseGapWindows = 10, int minimumPulseLength = 5)
        {
            double[] energySpectrum = ComputeEnergySpectrum();
            double baselineEnergy = energySpectrum[0];
            double energyThreshold = thresholdMultiplier * baselineEnergy; // Threshold can be adjusted

            List<int> potentialPulseIndices = new List<int>();
            List<int> pulseStartIndices = new List<int>();
            List<int> noiseDropIndices = new List<int>();

            // Identify potential pulse indices
            for (int windowIndex = 1; windowIndex < _totalWindows; windowIndex++)
            {
                double energyDifference = energySpectrum[windowIndex] - baselineEnergy;

                if (energyDifference > energyThreshold)
                {
                    potentialPulseIndices.Add(windowIndex);
                }
            }

            int consecutiveCount = 0;
            bool isFindingStart = true;

            // Determine pulse start indices
            for (int i = 0; i < potentialPulseIndices.Count - 1; i++)
            {
                if (potentialPulseIndices[i + 1] - potentialPulseIndices[i] > minimumPulseGapWindows)
                {
                    consecutiveCount = 0;
                    isFindingStart = true;
                    noiseDropIndices.Add(potentialPulseIndices[i + 1]);
                    continue;
                }

                consecutiveCount++;

                if (isFindingStart)
                {
                    if (consecutiveCount > minimumPulseLength)
                    {
                        isFindingStart = false;
                        pulseStartIndices.Add(potentialPulseIndices[i - minimumPulseLength]);
                    }
                }
            }

            // Keep only elements in pulseNoiseDropIndices that are NOT in pulseStartIndices
            noiseDropIndices = noiseDropIndices.Except(pulseStartIndices).ToList();

            // Output the detected pulse indices for debugging or further processing
            if (pulseStartIndices.Count > 0)
            {
                Console.WriteLine("Detected pulse windows: " + string.Join(", ", potentialPulseIndices));
                Console.WriteLine("Detected pulse start windows: " + string.Join(", ", pulseStartIndices));
                Console.WriteLine("Detected noise drop windows: " + string.Join(", ", noiseDropIndices));
            }
            else
            {
                Console.WriteLine("No significant pulses detected.");
            }

            // Convert window indices to sample indices in the original signal
            OriginalSignalData.PulseSampleIndices = ConvertWindowIndicesToSampleIndices(pulseStartIndices, sampleOffset);
            OriginalSignalData.NoiseDropSampleIndices = ConvertWindowIndicesToSampleIndices(noiseDropIndices);
        }

        private double[] ComputeEnergySpectrum()
        {
            double[][] windowedSegments = new double[_totalWindows][];
            double[] energySpectrum = new double[_totalWindows];

            for (int windowIndex = 0; windowIndex < _totalWindows; windowIndex++)
            {
                int segmentLength = Math.Min(OriginalSignalData.WindowSize, OriginalSignalData.TimeDomainSignal.Length - windowIndex * OriginalSignalData.WindowSize);
                double[] segment = new double[segmentLength];
                Array.Copy(OriginalSignalData.DecimalSignal, windowIndex * OriginalSignalData.WindowSize, segment, 0, segmentLength);
                windowedSegments[windowIndex] = segment;

                // Calculate energy as the sum of squares of the amplitudes
                foreach (double signalValue in segment)
                {
                    energySpectrum[windowIndex] += signalValue * signalValue;
                }

                // Optionally normalize the energy by the window size
                energySpectrum[windowIndex] /= OriginalSignalData.WindowSize;
            }
            return energySpectrum;
        }

        private int[] ConvertWindowIndicesToSampleIndices(List<int> windowIndices, int sampleOffset = 0)
        {
            List<int> sampleIndices = new List<int>();

            foreach (int windowIndex in windowIndices)
            {
                // Convert window index to sample index in the original time domain signal
                int sampleIndex = windowIndex * OriginalSignalData.WindowSize + sampleOffset;
                sampleIndices.Add(sampleIndex);
            }

            return sampleIndices.ToArray();
        }
    }
}
