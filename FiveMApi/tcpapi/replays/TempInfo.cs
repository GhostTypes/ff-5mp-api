using System;
using System.Diagnostics;

namespace FiveMApi.tcpapi.replays
{
    public class TempInfo
    {
        private TempData _extruderTemp;
        private TempData _bedTemp;


        public TempInfo FromReplay(string replay)
        {
            if (replay == null) return null;
            try
            {
                var data = replay.Split('\n');
                if (data.Length <= 1)
                {
                    Console.WriteLine("TempInfo replay has invalid data?: " + data);
                    return null;
                }

                var tempData = data[1].Split(' ');
                var e = tempData[0].Replace("T0:", "").Replace("/0.0", "");
                var b = tempData[2].Replace("B:", "").Replace("/0.0", "");
                _extruderTemp = new TempData(e);
                _bedTemp = new TempData(b);
                return this;
                //Console.WriteLine("Extruder temp is " + extruderTemp.GetFull());
                //Console.WriteLine("Bed temp is " + bedTemp.GetFull());
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to create TempInfo instance from replay");
                Debug.WriteLine(replay);
                Debug.WriteLine(e.StackTrace);
                return null;
            }
        }

        public TempData GetExtruderTemp()
        {
            return _extruderTemp;
        }

        public TempData GetBedTemp()
        {
            return _bedTemp;
        }

        public bool IsCooled()
        {
            return _bedTemp.GetCurrent() <= 40 && _extruderTemp.GetCurrent() <= 200;
        }

        public bool AreTempsSafe()
        {
            return _extruderTemp.GetCurrent() < 250 && _bedTemp.GetCurrent() < 100;
        }

        public class TempData
        {
            private readonly string _current;
            private readonly string _set;

            public TempData(string data)
            {
                if (data.Contains("/"))
                { // replay has current/set temps
                    var splitTemps = data.Split('/');
                    _current = ParseTdata(splitTemps[0].Trim());
                    _set = ParseTdata(splitTemps[1].Trim());
                }
                else
                { // replay only has current temp (when printer is idle)
                    _current = ParseTdata(data);
                    _set = null;
                }
            }

            private static string ParseTdata(string data)
            {
                if (data.Contains(".")) data = data.Split('.')[0].Trim();
                var temp = (int) Math.Round(double.Parse(data), MidpointRounding.AwayFromZero);
                return temp.ToString();
            }

            public string GetFull()
            {
                if (_set == null) return _current;
                return _current + "/" + _set;
            }

            public int GetCurrent() { return int.Parse(_current); }
            public int GetSet() { return int.Parse(_set); }
        }
    }
}