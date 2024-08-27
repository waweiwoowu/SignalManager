using SignalManager.AudioTools.AudioManager;
using SignalManager.AudioTools.DataManager;
using SignalManager.ProcessingTools.FourierTransforms;
using System;
using System.Linq;

namespace SignalManager.AudioTools.AnalyzingManager
{
    public class SignalAnalyzer
    {
        private AudioReader AudioReader;
        public SignalData Data { get; set; }

        public SignalAnalyzer(string audioFilePath)
        {
            AudioReader = new AudioReader(audioFilePath);
            Data = AudioReader.Data;
            Data.TimeDomainSignal = Data.DecimalSignal;
        }

        public (double[] FrequencyBins, double[] MagnitudeDifferences) CompareWithBaseline(SignalData baselineData, double frequencyBinInterval)
        {
            if (this.Data.SampleRate != baselineData.SampleRate)
            {
                throw new Exception("Sample rates do not match.");
            }

            if (this.Data.DecimalSignal.Length != baselineData.DecimalSignal.Length)
            {
                throw new Exception("Signal lengths do not match.");
            }

            // Compute the magnitude spectrum for both the target and baseline samples
            var (frequencyBins, targetMagnitude) = this.Data.RFFT.ComputeMagnitudeInFrequencyBinInterval(
                Data.Magnitude, Data.SampleRate, Data.NumberOfSamples, frequencyBinInterval);

            var (_, baselineMagnitude) = baselineData.RFFT.ComputeMagnitudeInFrequencyBinInterval(
                baselineData.Magnitude, baselineData.SampleRate, baselineData.NumberOfSamples, frequencyBinInterval);

            // Calculate the difference in magnitude between the two spectra
            double[] magnitudeDifferences = new double[targetMagnitude.Length];
            for (int i = 0; i < targetMagnitude.Length; i++)
            {
                magnitudeDifferences[i] = Math.Max(0, targetMagnitude[i] - baselineMagnitude[i]);
            }

            return (frequencyBins, magnitudeDifferences);
        }

        public List<(double Frequency, double Magnitude)> CompareCharacteristicFrequencies(SignalData baselineData, double frequencyBinInterval, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            var (frequencyBins, magnitudeDifferences) = CompareWithBaseline(baselineData, frequencyBinInterval);

            return ExtractCharacteristicFrequencies(frequencyBins, magnitudeDifferences, peakFrequencyCount, minFrequency, maxFrequency);
        }

        public List<(double Frequency, double Magnitude)> ExtractCharacteristicFrequencies(double[] frequencyBins, double[] magnitudeDifferences, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            // Identify characteristic frequencies from the magnitude differences
            var (characteristicFrequencies, characteristicMagnitudes) = this.Data.RFFT.FindCharacteristicFrequencies(frequencyBins, magnitudeDifferences, peakFrequencyCount, minFrequency, maxFrequency);

            // Create a list to store the result
            var result = new List<(double, double)>();

            for (int i = 0; i < characteristicFrequencies.Length; i++)
            {
                result.Add((characteristicFrequencies[i], characteristicMagnitudes[i]));
            }

            return result;
        }
    }
}
