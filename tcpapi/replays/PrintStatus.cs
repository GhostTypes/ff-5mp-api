using System;
using System.Diagnostics;

namespace FiveMApi.tcpapi.replays
{
    public class PrintStatus
    {
        private string _sdCurrent;
        private string _sdTotal;
        private string _layerCurrent;
        private string _layerTotal;

        public PrintStatus FromReplay(string replay)
        {
            //Console.WriteLine("PrintStatus replay:\n" + replay);
            var data = replay.Split('\n');

            // this should be safe because it returns default values when not printing
            var sdProgress = data[1].Replace("SD printing byte ", "").Trim();
            var sdProgressData = sdProgress.Split('/');
            _sdCurrent = sdProgressData[0].Trim();
            _sdTotal = sdProgressData[1].Trim();

            // added all this bullshit because it started throwing index out of bounds,
            // hopefully can narrow it down, and it doesn't start happening to sdProgress
            string layerProgress;
            try
            {
                layerProgress = data[2].Replace("Layer: ", "").Trim();
            }
            catch (Exception e)
            {
                Debug.WriteLine("PrintStatus bad layer progress bullshit pt1");
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
                Debug.WriteLine("PrintStatus bad layer progress bullshit pt2");
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