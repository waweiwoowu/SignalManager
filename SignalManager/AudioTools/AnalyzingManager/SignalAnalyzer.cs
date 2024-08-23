using SignalManager.AudioTools.AudioManager;
using SignalManager.AudioTools.DataManager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
