using System;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace FiveMApi.tcpapi.replays
{
    public class EndstopStatus
    {
        public Endstop _Endstop;
        public MachineStatus _MachineStatus;
        public MoveMode _MoveMode;
        public Status _Status;
        public bool _LedEnabled;
        public string _CurrentFile;
        
        public EndstopStatus FromReplay(string replay)
        {
            if (replay == null) return null;
            try
            {
                var data = replay.Split('\n');
                _Endstop = new Endstop(data[1]);
                var machineStatus = data[2].Replace("MachineStatus: ", "").Trim();
                if (machineStatus.Contains("BUILDING_FROM_SD")) _MachineStatus = MachineStatus.BUILDING_FROM_SD;
                else if (machineStatus.Contains("BUILDING_COMPLETED")) _MachineStatus = MachineStatus.BUILDING_COMPLETED;
                else if (machineStatus.Contains("PAUSED")) _MachineStatus = MachineStatus.PAUSED;
                else if (machineStatus.Contains("READY")) _MachineStatus = MachineStatus.READY;
                else
                {
                    Debug.WriteLine("EndstopStatus Encountered unknown MachineStatus: " + machineStatus);
                    _MachineStatus = MachineStatus.DEFAULT;
                }
                var moveM = data[3].Replace("MoveMode: ", "").Trim();
                if (moveM.Contains("MOVING")) _MoveMode = MoveMode.MOVING;
                else if (moveM.Contains("PAUSED")) _MoveMode = MoveMode.PAUSED;
                else if (moveM.Contains("READY")) _MoveMode = MoveMode.READY;
                else
                {
                    Debug.WriteLine("EndstopStatus Encountered unknown MoveMode: " + moveM);
                    _MoveMode = MoveMode.DEFAULT;
                }
                _Status = new Status(data[4]);
                _LedEnabled = int.Parse(data[5].Replace("LED: ", "").Trim()) == 1;
                _CurrentFile = data[6].Replace("CurrentFile: ", "")/*.Replace(".gx", "").Replace(".gcode", "")*/.TrimEnd();
                if (string.IsNullOrWhiteSpace(_CurrentFile)) _CurrentFile = null;
                return this;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Unable to create EndstopStatus instance from replay");
                Debug.WriteLine(replay);
                Debug.WriteLine(e.StackTrace);
                return null;
            }
        }

        public class Status
        {
            public int S { get; set; }
            public int L { get; set; }
            public int J { get; set; }
            public int F { get; set; }

            public Status(string data)
            {
                S = GetValue(data, "S");
                L = GetValue(data, "L");
                J = GetValue(data, "J");
                F = GetValue(data, "F");
            }
        }

        public class Endstop
        {
            public int Xmax { get; set; }
            public int Ymax { get; set; }
            public int Zmin { get; set; }

            public Endstop(string data)
            {
                Xmax = GetValue(data, "X-max");
                Ymax = GetValue(data, "Y-max");
                Zmin = GetValue(data, "Z-min");
            }
        }
        
        private static int GetValue(string input, string key)
        {
            var pattern = key + @":(\d+)";
            var match = Regex.Match(input, pattern);
            if (match.Success) return int.Parse(match.Groups[1].Value);
            return -1;
        }

        public bool IsPrintComplete() { return _MachineStatus == MachineStatus.BUILDING_COMPLETED; }
        public bool IsPrinting() { return _MachineStatus == MachineStatus.BUILDING_FROM_SD; }
        public bool IsReady() { return _MoveMode == MoveMode.READY && _MachineStatus == MachineStatus.READY; }
        public bool IsPaused() { return _MachineStatus == MachineStatus.PAUSED || _MoveMode == MoveMode.PAUSED; } //todo what's the difference between the two states?
    }
    
    public enum MachineStatus { BUILDING_FROM_SD, BUILDING_COMPLETED, PAUSED, READY, DEFAULT }

    public enum MoveMode { MOVING, PAUSED, READY, DEFAULT }
}