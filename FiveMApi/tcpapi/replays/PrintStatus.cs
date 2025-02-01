using System;
using System.Diagnostics;

namespace FiveMApi.tcpapi.replays
{
    /// <summary>
    /// Deprecated
    /// </summary>
    public class PrintStatus
    {
        // This shouldn't be used by anything. Rely on the new api
        private string _sdCurrent;
        private string _sdTotal;
        private string _layerCurrent;
        private string _layerTotal;

        public PrintStatus FromReplay(string replay)
        {
            var data = replay.Split('\n');
            var sdProgress = data[1].Replace("SD printing byte ", "").Trim();
            var sdProgressData = sdProgress.Split('/');
            _sdCurrent = sdProgressData[0].Trim();
            _sdTotal = sdProgressData[1].Trim();
            
            string layerProgress;
            try { layerProgress = data[2].Replace("Layer: ", "").Trim(); }
            catch (Exception e)
            {
                Debug.WriteLine("PrintStatus bad layer progress");
                Debug.WriteLine("Raw printer replay: " + replay);
                Debug.WriteLine(e.StackTrace);
                return null;
            }
            try
            {
                var lpData = layerProgress.Split('/');
                _layerCurrent = lpData[0].Trim();
                _layerTotal = lpData[1].Trim();
                return this;
            }
            catch (Exception e)
            {
                Debug.WriteLine("PrintStatus bad layer progress");
                Debug.WriteLine("layerProgress: " + layerProgress);
                Debug.WriteLine(e.StackTrace);
                return null;
            }
        }

        public int GetPrintPercent()
        {
            var current = int.Parse(_sdCurrent);
            var total = int.Parse(_sdTotal);
            var perc = (current / (double)total) * 100;
            return (int) Math.Round(perc, MidpointRounding.AwayFromZero);
        }

        public string GetLayerProgress() { return _layerCurrent + "/" + _layerTotal; }

        public string GetSdProgress() { return _sdCurrent + "/" + _sdTotal; }
    }
}