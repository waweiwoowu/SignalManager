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

        public (double[], double[]) Compare(SignalAnalyzer goodSample, double frequencyBinInterval)
        {
            if (this.SignalData.SampleRate != goodSample.SignalData.SampleRate)
            {
                throw new Exception("Sample rates do not match.");
            }

            if (this.SignalData.DecimalSignal.Length != goodSample.SignalData.DecimalSignal.Length)
            {
                throw new Exception("Signal lengths do not match.");
            }

            // Compute the magnitude spectrum for both the target and good samples
            var (frequencyBins, magnitude) = SignalData.RFFT.ComputeMagnitudeInFrequencyBinInterval(
                SignalData.Magnitude, SignalData.SampleRate, SignalData.TimeDomainSignal.Length, frequencyBinInterval);

            var (_, goodSampleMagnitude) = goodSample.SignalData.RFFT.ComputeMagnitudeInFrequencyBinInterval(
                goodSample.SignalData.Magnitude, goodSample.SignalData.SampleRate, goodSample.SignalData.TimeDomainSignal.Length, frequencyBinInterval);

            // Compare the two magnitude spectra
            double[] result = new double[magnitude.Length];
            for (int i = 0; i < magnitude.Length; i++)
            {
                result[i] = Math.Abs(magnitude[i] - goodSampleMagnitude[i]);
            }

            return (frequencyBins, result);
        }

        public List<(double frequency, double difference)> CompareCharacteristicFrequencies(SignalAnalyzer goodSampleAnalyzer, double frequencyBinInterval, int numberOfFrequencies)
        {
            // Find characteristic frequencies of the good sample
            var (goodFrequencies, goodMagnitudes) = goodSampleAnalyzer.SignalData.RFFT.FindCharacteristicFrequencies(goodSampleAnalyzer.SignalData.Magnitude, frequencyBinInterval, numberOfFrequencies);

            // List to store the (frequency, difference) pairs
            List<(double frequency, double difference)> frequencyDifferences = new List<(double frequency, double difference)>();

            // Calculate the magnitude of the target signal at these frequencies and store the differences
            foreach (var goodFreq in goodFrequencies)
            {
                var frequencyBinsWithInterval = SignalData.RFFT.GetFrequencyBinsWithInterval(frequencyBinInterval);
                int targetIndex = Array.IndexOf(frequencyBinsWithInterval, goodFreq);

                if (targetIndex >= 0 && targetIndex < SignalData.Magnitude.Length)
                {
                    double targetMagnitude = SignalData.Magnitude[targetIndex];
                    double goodMagnitude = goodMagnitudes[Array.IndexOf(goodFrequencies, goodFreq)];

                    double difference = Math.Abs(targetMagnitude - goodMagnitude);

                    // Add the frequency and difference pair to the list
                    frequencyDifferences.Add((goodFreq, difference));
                }
            }

            return frequencyDifferences;
        }

    }
}
