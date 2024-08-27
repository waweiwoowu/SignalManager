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
        public FFTCalculator RFFT { get; private set; }

        private int _windowSize = 4096;
        private int _hopSize = 2048;
        private double[] _timeDomainSignal;
        private Complex[] _frequencyDomainSignal;
        private double[] _timeBins;
        private double[] _frequencyBins;
        private double[] _amplitude;
        private double[] _magnitude;
        private double[] _amplitudeDB;
        private double[] _magnitudeDB;
        private Complex[][] _frequencySpectrum;
        private double[][] _magnitudeSpectrum;
        private double[] _averageMagnitudeSpectrum;
        private double[] _averageMagnitudeDBSpectrum;

        public double[] TimeDomainSignal 
        { 
            get { return _timeDomainSignal; } 
            set { _timeDomainSignal = value; Reset(); }
        }

        // FFT Data
        public Complex[] FrequencyDomainSignal => _frequencyDomainSignal ?? (_frequencyDomainSignal = FFT.ComputeFrequencyDomainSignal(TimeDomainSignal));
        public double[] TimeBins => _timeBins ?? (_timeBins = STFT.ComputeTimeBins(TimeDomainSignal, SampleRate));
        public double[] FrequencyBins => _frequencyBins ?? (_frequencyBins = FFT.ComputeFrequencyBins());
        public double[] Amplitude => _amplitude ?? (_amplitude = STFT.ComputeAmplitude(TimeDomainSignal));
        public double[] Magnitude => _magnitude ?? (_magnitude = FFT.ComputeMagnitude(FrequencyDomainSignal));
        public double[] AmplitudeDB => _amplitudeDB ?? (_amplitudeDB = STFT.ComputeMagnitudeInDb(Amplitude));
        public double[] MagnitudeDB => _magnitudeDB ?? (_magnitudeDB = STFT.ComputeMagnitudeInDb(Magnitude));

        // STFT Data
        public int WindowSize { get { return _windowSize; } set { _windowSize = value; } }
        public int HopSize { get { return _hopSize; } set { _hopSize = value; } }
        public int PulseWidth { get; set; }
        public int[] PulseSampleIndices { get; set; }
        public int[] NoiseDropSampleIndices { get; set; }
        public Complex[][] FrequencyDomainSignalSpectrum
        {
            get
            {
                _frequencySpectrum = STFT.ComputeFrequencyDomainSpectrum(TimeDomainSignal);
                return _frequencySpectrum;
            }
            set
            {
                _frequencySpectrum = value;
            }
        }
        public double[][] MagnitudeSpectrum
        {
            get
            {
                _magnitudeSpectrum = STFT.ComputeMagnitudeSpectrum(TimeDomainSignal);
                return _magnitudeSpectrum;
            }
            set
            {
                _magnitudeSpectrum = value;
            }
        }
        public double[] AverageMagnitudeSpectrum => _averageMagnitudeSpectrum ?? (_averageMagnitudeSpectrum = STFT.ComputeAverageMagnitude(MagnitudeSpectrum));
        public double[] AverageMagnitudeDBSpectrum => _averageMagnitudeDBSpectrum ?? (_averageMagnitudeDBSpectrum = STFT.ComputeMagnitudeInDb(AverageMagnitudeSpectrum));

        private void Reset()
        {
            _frequencyDomainSignal = null;
            _timeBins = null;
            _frequencyBins = null;
            _amplitude = null;
            _magnitude = null;
            _amplitudeDB = null;
            _magnitudeDB = null;
            _frequencySpectrum = null;
            _magnitudeSpectrum = null;
            _averageMagnitudeSpectrum = null;
            _averageMagnitudeDBSpectrum = null;

            STFT = new STFTCalculator(TimeDomainSignal, SampleRate, WindowSize, HopSize);
            FFT = new FFTCalculator(TimeDomainSignal, SampleRate);
            RFFT = new RFFTCalculator(TimeDomainSignal, SampleRate);
        }
    }
}
