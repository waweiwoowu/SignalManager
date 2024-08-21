using ConfigManager;
using SignalManager.AudioTools.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace SignalManager.AudioTools.FileManager
{
    public class SignalFileManager
    {
        public SignalData SignalData;
        private JsonFileHandler jsonFileHandler;
        private string section = "";

        public SignalFileManager(string filePath)
        {
            SignalData = new SignalData();
            jsonFileHandler = new JsonFileHandler(filePath);
            Read();
        }

        public void Read()
        {
            ReadAudioData();
            ReadSignalData();
        }

        private void ReadAudioData()
        {
            section = SignalFileConfig.Audio.Section;
            SignalData.Channels = jsonFileHandler.Get<int>(section, SignalFileConfig.Audio.Channels, 0);
            SignalData.SampleWidth = jsonFileHandler.Get<int>(section, SignalFileConfig.Audio.SampleWidth, 0);
            SignalData.SampleRate = jsonFileHandler.Get<int>(section, SignalFileConfig.Audio.SampleRate, 0);
            SignalData.NumberOfSamples = jsonFileHandler.Get<int>(section, SignalFileConfig.Audio.NumberOfSamples, 0);
            SignalData.DecimalSignal = jsonFileHandler.GetArray<double>(section, SignalFileConfig.Audio.DecimalSignal, new double[0]);
        }
        private void ReadSignalData()
        {
            section = SignalFileConfig.Signal.Section;
            SignalData.PulseWidth = jsonFileHandler.Get<int>(section, SignalFileConfig.Signal.PulseWidth, 0);
            SignalData.PulseSampleIndices = jsonFileHandler.GetArray<int>(section, SignalFileConfig.Signal.PulseSampleIndices, new int[0]);
            SignalData.NoiseDropSampleIndices = jsonFileHandler.GetArray<int>(section, SignalFileConfig.Signal.NoiseDropSampleIndices, new int[0]);
            SignalData.TimeDomainSignal = jsonFileHandler.GetArray<double>(section, SignalFileConfig.Signal.TimeDomainSignal, SignalData.DecimalSignal);
            //SignalData.FrequencyDomainSignalSpectrum = jsonFileHandler.GetArray<Complex>(section, SignalFileConfig.Signal.FrequencyDomainSignalSpectrum, new Complex[0][]);
            //SignalData.MagnitudeSpectrum = jsonFileHandler.GetArray<double>(section, SignalFileConfig.Signal.MagnitudeSpectrum, new double[0][]);
        }

        public void Write()
        {
            section = SignalFileConfig.Signal.Section;
            jsonFileHandler.Set<int>(section, SignalFileConfig.Signal.PulseWidth, SignalData.PulseWidth);
            jsonFileHandler.SetArray<int>(section, SignalFileConfig.Signal.PulseSampleIndices, SignalData.PulseSampleIndices);
            jsonFileHandler.SetArray<int>(section, SignalFileConfig.Signal.NoiseDropSampleIndices, SignalData.NoiseDropSampleIndices);
            //jsonFileHandler.SetArray<double>(section, SignalFileConfig.Signal.TimeDomainSignal, SignalData.TimeDomainSignal);
            //jsonFileHandler.SetArray<Complex>(section, SignalFileConfig.Signal.FrequencyDomainSignalSpectrum, SignalData.FrequencyDomainSignalSpectrum);
            //jsonFileHandler.SetArray<double>(section, SignalFileConfig.Signal.MagnitudeSpectrum, SignalData.MagnitudeSpectrum);
            jsonFileHandler.SaveFile();
        }
    }
}
