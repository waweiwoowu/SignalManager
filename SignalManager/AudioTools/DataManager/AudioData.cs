using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.AudioTools.DataManager
{
    public class AudioData
    {
        // Properties representing the audio signal characteristics
        public int Channels { get; set; }
        public int SampleWidth { get; set; }
        public int SampleRate { get; set; }
        public int NumberOfSamples { get; set; }
        public byte[] ByteSignal { get; set; }
        public double[] DecimalSignal { get; set; }
        public double[] NormalizedSignal { get; set; }
    }
}
