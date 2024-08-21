using SignalManager.ProcessingTools.FourierTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.AudioTools.DataManager
{
    public class SignalData : AudioData
    {
        public STFTCalculator STFT { get; private set; }
        public FFTCalculator FFT { get; private set; }

        private double[] _timeDomainSignal;
        private Complex[] _frequencyDomainSignal;
        private double[] _timeBins;
        private double[] _frequencyBins;
        private double[] _amplitude;
        private double[] _magnitude;
        private double[] _amplitudeDB;
        private double[] _magnitudeDB;
        private int _windowSize = 4096;
        private int _hopSize = 2048;

        public double[] TimeDomainSignal 
        { 
            get { return _timeDomainSignal; } 
            set { _timeDomainSignal = value; Reset(); }
        }

        // FFT Data
        public Complex[] FrequencyDomainSignal => _frequencyDomainSignal ?? (_frequencyDomainSignal = FFT.ComputeFrequencyDomainSignal(TimeDomainSignal));
        public double[] TimeBins => _timeBins ?? (_timeBins = STFT.ComputeTimeBins(TimeDomainSignal, SampleRate));
        public double[] FrequencyBins => _frequencyBins ?? (_frequencyBins = FFT.ComputeFrequencyBins(SampleRate, TimeDomainSignal.Length));
        public double[] Amplitude => _amplitude ?? (_amplitude = STFT.ComputeAmplitude(TimeDomainSignal));
        public double[] Magnitude => _magnitude ?? (_magnitude = FFT.ComputeMagnitude(FrequencyDomainSignal));
        public double[] AmplitudeDB => _amplitudeDB ?? (_amplitudeDB = STFT.MagnitudeToDb(Amplitude));
        public double[] MagnitudeDB => _magnitudeDB ?? (_magnitudeDB = STFT.MagnitudeToDb(Magnitude));

        // STFT Data
        public int WindowSize { get { return _windowSize; } set { _windowSize = value; } }
        public int HopSize { get { return _hopSize; } set { _hopSize = value; } }
        public int PulseWidth { get; set; }
        public int[] PulseSampleIndices { get; set; }
        public int[] NoiseDropSampleIndices { get; set; }
        public Complex[][] FrequencyDomainSignalSpectrum { get; set; }
        public double[][] MagnitudeSpectrum { get; set; }

        //public double[][] MagnitudeDBSpectrum { get; set; }

        private void Reset()
        {
            _frequencyDomainSignal = null;
            _timeBins = null;
            _frequencyBins = null;
            _amplitude = null;
            _magnitude = null;
            STFT = new STFTCalculator(TimeDomainSignal, SampleRate, WindowSize, HopSize);
            FFT = new FFTCalculator(TimeDomainSignal, SampleRate);
        }
    }
}
