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
        public SignalData SignalData { get; set; } = new SignalData();

        public SignalAnalyzer(string audioFilePath)
        {
            AudioReader = new AudioReader(audioFilePath);
            SignalData = AudioReader.Data;
            SignalData.TimeDomainSignal = SignalData.DecimalSignal;
        }

        public (double[] combinedFrequencyBins, double[] comparedMagnitude) Compare(SignalData baselineSampleData, double frequencyBinInterval)
        {
            if (this.SignalData.SampleRate != baselineSampleData.SampleRate)
            {
                throw new Exception("Sample rates do not match.");
            }

            if (this.SignalData.DecimalSignal.Length != baselineSampleData.DecimalSignal.Length)
            {
                throw new Exception("Signal lengths do not match.");
            }

            // Compute the magnitude spectrum for both the target and good samples
            var (combinedFrequencyBins, magnitude) = this.SignalData.RFFT.ComputeMagnitudeInFrequencyBinInterval(
                SignalData.Magnitude, SignalData.SampleRate, SignalData.TimeDomainSignal.Length, frequencyBinInterval);

            var (_, goodSampleMagnitude) = baselineSampleData.RFFT.ComputeMagnitudeInFrequencyBinInterval(
                baselineSampleData.Magnitude, baselineSampleData.SampleRate, baselineSampleData.TimeDomainSignal.Length, frequencyBinInterval);

            // Compare the two magnitude spectra
            double[] comparedMagnitude = new double[magnitude.Length];
            double difference = 0;
            for (int i = 0; i < magnitude.Length; i++)
            {
                difference = magnitude[i] - goodSampleMagnitude[i];
                if (difference > 0)
                    comparedMagnitude[i] = difference;
            }

            return (combinedFrequencyBins, comparedMagnitude);
        }

        public List<(double comparedCharacteristicFrequencies, double comparedCharacteristicMagnitudes)> CompareCharacteristicFrequencies(SignalData baselineSampleData, double frequencyBinInterval, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            var (combinedFrequencyBins, comparedMagnitude) = Compare(baselineSampleData, frequencyBinInterval);

            return CompareCharacteristicFrequencies(combinedFrequencyBins, comparedMagnitude, peakFrequencyCount, minFrequency, maxFrequency);
        }

        public List<(double comparedCharacteristicFrequencies, double comparedCharacteristicMagnitudes)> CompareCharacteristicFrequencies(double[] combinedFrequencyBins, double[] comparedMagnitude, int peakFrequencyCount, double minFrequency = 0, double maxFrequency = double.MaxValue)
        {
            // Find characteristic frequencies of the compared signal
            var (comparedCharacteristicFrequencies, comparedCharacteristicMagnitudes) = this.SignalData.RFFT.FindCharacteristicFrequencies(combinedFrequencyBins, comparedMagnitude, peakFrequencyCount, minFrequency, maxFrequency);

            // Create a list to store the result
            var result = new List<(double, double)>();

            for (int i = 0; i < comparedCharacteristicFrequencies.Length; i++)
            {
                result.Add((comparedCharacteristicFrequencies[i], comparedCharacteristicMagnitudes[i]));
            }

            return result;
        }
    }
}
