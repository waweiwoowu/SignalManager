using NAudio.Wave;
using SignalManager.AudioTools.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.AudioTools.AudioManager
{
    public class AudioReader
    {
        public SignalData Data;
        public AudioReader(string audioFilePath) 
        {
            Data = new SignalData();
            using (var reader = new WaveFileReader(audioFilePath))
            {
                Data.Channels = reader.WaveFormat.Channels;
                Data.SampleWidth = reader.WaveFormat.BitsPerSample / 8;
                Data.SampleRate = reader.WaveFormat.SampleRate;
                Data.NumberOfSamples = (int)reader.SampleCount;

                Data.ByteSignal = new byte[Data.NumberOfSamples * Data.Channels * Data.SampleWidth];
                reader.Read(Data.ByteSignal, 0, Data.ByteSignal.Length);
            }
            Data.DecimalSignal = ConvertToDecimalSignal(Data.ByteSignal, Data.SampleWidth, Data.Channels);
            Data.TimeDomainSignal = Data.DecimalSignal;
        }

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
