using System;
using System.IO;
using System.Linq;
using NAudio.Wave;
using ConfigManager;
using SignalManager.AudioTools.DataManager;
using System.Threading.Channels;

namespace SignalManager.AudioTools.FileManager
{
    public class AudioFileManager
    {
        private JsonFileHandler jsonFileHandler;
        public AudioData AudioData;

        public AudioFileManager(string filePath)
        {
            AudioData = new AudioData();
            ReadSignalData(filePath);
        }

        private void ReadSignalData(string filePath)
        {
            using (var reader = new WaveFileReader(filePath))
            {
                AudioData.Channels = reader.WaveFormat.Channels;
                AudioData.SampleWidth = reader.WaveFormat.BitsPerSample / 8;
                AudioData.SampleRate = reader.WaveFormat.SampleRate;
                AudioData.NumberOfSamples = (int)reader.SampleCount;

                AudioData.ByteSignal = new byte[AudioData.NumberOfSamples * AudioData.Channels * AudioData.SampleWidth];
                reader.Read(AudioData.ByteSignal, 0, AudioData.ByteSignal.Length);
            }
            AudioData.DecimalSignal = ConvertToDecimalSignal(AudioData.ByteSignal, AudioData.SampleWidth, AudioData.Channels);
            //SignalData.NormalizedSignal = NormalizeSignal(SignalData.DecimalSignal);
        }

        public void WriteSignalDataToJson(string jsonFilePath)
        {
            jsonFileHandler = new JsonFileHandler(jsonFilePath);

            string section = SignalFileConfig.Audio.Section;
            jsonFileHandler.Set(section, SignalFileConfig.Audio.Channels, AudioData.Channels);
            jsonFileHandler.Set(section, SignalFileConfig.Audio.SampleWidth, AudioData.SampleWidth);
            jsonFileHandler.Set(section, SignalFileConfig.Audio.SampleRate, AudioData.SampleRate);
            jsonFileHandler.Set(section, SignalFileConfig.Audio.NumberOfSamples, AudioData.NumberOfSamples);
            //jsonFileHandler.SetArray(section, FileConfig.SignalData.ByteSignal, SignalData.ByteSignal);
            jsonFileHandler.SetArray(section, SignalFileConfig.Audio.DecimalSignal, AudioData.DecimalSignal);
            //jsonFileHandler.SetArray(section, FileConfig.SignalData.NormalizedSignal, SignalData.NormalizedSignal);
            
            jsonFileHandler.SaveFile();
        }

        // Method to convert byte signal to decimal signal
        private double[] ConvertToDecimalSignal(byte[] byteSignal, int sampleWidth, int channels)
        {
            var decimalSignal = new double[byteSignal.Length / sampleWidth];

            if (sampleWidth == 2) // 16-bit PCM
            {
                for (int i = 0; i < byteSignal.Length; i += 2)
                {
                    decimalSignal[i / 2] = BitConverter.ToInt16(byteSignal, i) / 32768f;
                }
            }
            else if (sampleWidth == 1) // 8-bit PCM
            {
                for (int i = 0; i < byteSignal.Length; i++)
                {
                    decimalSignal[i] = (byteSignal[i] - 128) / 128f;
                }
            }

            if (channels > 1)
            {
                decimalSignal = decimalSignal.Where((x, i) => i % channels == 0).ToArray();
            }

            return decimalSignal;
        }

        private double[] NormalizeSignal(double[] decimalSignal)
        {
            double maxSampleValue = 0;

            // Find the maximum sample value for normalization
            foreach (var sample in decimalSignal)
            {
                if (Math.Abs(sample) > maxSampleValue)
                {
                    maxSampleValue = Math.Abs(sample);
                }
            }

            // Normalize the samples to the range -1.0 to 1.0
            for (int i = 0; i < decimalSignal.Length; i++)
            {
                decimalSignal[i] /= maxSampleValue;
            }

            return decimalSignal;
        }
    }
}