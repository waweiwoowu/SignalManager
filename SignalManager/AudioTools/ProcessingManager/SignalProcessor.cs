using System;
using System.Collections.Generic;
using System.Linq;
using NAudio.Wave;
using SignalManager.AudioTools.DataManager;

namespace SignalManager.AudioTools.ProcessingManager
{
    public partial class SignalProcessor
    {
        public SignalData OriginalSignalData { get; set; }
        public SignalData NoiseSignalData { get; set; } = new SignalData();
        public List<SignalData> PulseSignalDataList { get; set; } = new List<SignalData>();

        public SignalProcessor(SignalData signalData)
        {
            OriginalSignalData = signalData;
            OriginalSignalData.TimeDomainSignal = signalData.DecimalSignal;
            InitializeSignalProperties(NoiseSignalData);
            _totalWindows = (int)Math.Ceiling((double)OriginalSignalData.TimeDomainSignal.Length / OriginalSignalData.WindowSize);
        }

        private void InitializeSignalProperties(SignalData signalData) 
        {
            signalData.SampleRate = OriginalSignalData.SampleRate;
            signalData.WindowSize = OriginalSignalData.WindowSize;
            signalData.HopSize = OriginalSignalData.HopSize;
        }

        public void SaveSignalAsWavFile(double[] signalDataArray, string filePath)
        {
            if (!filePath.EndsWith(".wav")) filePath += ".wav";

            // Convert double[] to float[]
            float[] floatSignalDataArray = signalDataArray.Select(s => (float)s).ToArray();

            // Create a WaveFormat object with the given sample rate and sample width
            var waveFormat = new WaveFormat(OriginalSignalData.SampleRate, OriginalSignalData.SampleWidth * 8, 1);

            // Use WaveFileWriter to write the float array to the file
            using (var writer = new WaveFileWriter(filePath, waveFormat))
            {
                writer.WriteSamples(floatSignalDataArray, 0, floatSignalDataArray.Length);
            }
        }

        public void ExtractAndSavePulses(string outputDirectry, string baseFileName)
        {
            for (int i = 0; i < OriginalSignalData.PulseSampleIndices.Length; i++)
            {
                SignalData pulseData = new SignalData();
                PulseSignalDataList.Add(pulseData);
                InitializeSignalProperties(pulseData);

                pulseData.TimeDomainSignal = new double[OriginalSignalData.PulseWidth];
                Array.Copy(OriginalSignalData.TimeDomainSignal, OriginalSignalData.PulseSampleIndices[i], pulseData.TimeDomainSignal, 0, OriginalSignalData.PulseWidth);
                string filePath = Path.Combine(outputDirectry, $"{baseFileName}_{i}");
                SaveSignalAsWavFile(pulseData.TimeDomainSignal, filePath);
            }
        }
    }
}
