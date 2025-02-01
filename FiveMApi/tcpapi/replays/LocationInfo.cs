using System;
using System.Diagnostics;

namespace FiveMApi.tcpapi.replays
{
    public class LocationInfo
    {
        public string X { get; set; }
        public string Y { get; set; }
        public string Z { get; set; }
        
        public LocationInfo FromReplay(string replay)
        {
            try
            {
                var data = replay.Split('\n');
                var locData = data[1].Split(' ');
                X = locData[0].Replace("X:", "").Trim();
                Y = locData[1].Replace("Y:", "").Trim();
                Z = locData[2].Replace("Z:", "").Trim();
                return this;
            }
            catch (Exception e)
            {
                Debug.WriteLine("LocationInfo replay has bad/null data");
                Debug.WriteLine(e.StackTrace);
                return null;
            }
        }
        
        public override string ToString()
        {
            return "X: " + X + " Y: " + Y + " Z: " + Z;
        }
        
    }
}