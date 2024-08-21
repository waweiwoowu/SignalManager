using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.AudioTools.FileManager
{
    internal static class SignalFileConfig
    {
        public static class Audio
        {
            public static readonly string Section = "Audio Data";
            public static readonly string Channels = "Channels";
            public static readonly string SampleWidth = "Sample Width";
            public static readonly string SampleRate = "Sample Rate";
            public static readonly string NumberOfSamples = "Number Of Samples";
            public static readonly string ByteSignal = "Byte Signal";
            public static readonly string DecimalSignal = "Decimal Signal";
            public static readonly string NormalizedSignal = "Normalized Signal";
        }

        public static class Signal
        {
            public static readonly string Section = "Signal Data";
            public static readonly string PulseWidth = "PulseWidth";
            public static readonly string PulseSampleIndices = "Pulse Sample Indices";
            public static readonly string NoiseDropSampleIndices = "Noise Drop Sample Indices";
            public static readonly string TimeDomainSignal = "Time Domain Signal";
            public static readonly string FrequencyDomainSignalSpectrum = "Frequency Domain Signal Spectrum";
            public static readonly string MagnitudeSpectrum = "Magnitude Spectrum";
        }
    }
}
