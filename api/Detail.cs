namespace FiveMApi.api
{
    public partial class FiveMClient
    {
        public class Detail
        {
            public string AutoShutdown { get; set; }
            public int AutoShutdownTime { get; set; }
            public string CameraStreamUrl { get; set; }
            public int ChamberFanSpeed { get; set; }
            public int ChamberTargetTemp { get; set; } // not sure why these are in the fw, there's no chamber temp..
            public int ChamberTemp { get; set; }
            public int CoolingFanSpeed { get; set; }
            public double CumulativeFilament { get; set; }
            public int CumulativePrintTime { get; set; }
            public int CurrentPrintSpeed { get; set; }
            public string DoorStatus { get; set; } // broken and/or not implented
            public string ErrorCode { get; set; } // need more info
            public int EstimatedLeftLen { get; set; } // no left extruder..
            public double EstimatedLeftWeight { get; set; }
            public double EstimatedRightLen { get; set; }
            public double EstimatedRightWeight { get; set; }
            public double EstimatedTime { get; set; }
            public string ExternalFanStatus { get; set; }
            public int FillAmount { get; set; }
            public string FirmwareVersion { get; set; }
            public string FlashRegisterCode { get; set; }
            public string InternalFanStatus { get; set; }
            public string IpAddr { get; set; }
            public string LeftFilamentType { get; set; } // no left extruder..
            public int LeftTargetTemp { get; set; }
            public int LeftTemp { get; set; }
            public string LightStatus { get; set; }
            public string Location { get; set; }
            public string MacAddr { get; set; }
            public string Measure { get; set; } // 220x220x220
            public string Name { get; set; }
            public int NozzleCnt { get; set; } // always 1..
            public string NozzleModel { get; set; }
            public int NozzleStyle { get; set; } // not sure what the numbers translate to

            public int Pid { get; set; }
            public double PlatTargetTemp { get; set; }
            public double PlatTemp { get; set; }
            public string PolarRegisterCode { get; set; }
            public int PrintDuration { get; set; }
            public string PrintFileName { get; set; }
            public string PrintFileThumbUrl { get; set; }
            public int PrintLayer { get; set; }
            public double PrintProgress { get; set; }
            public double PrintSpeedAdjust { get; set; } // Changed from int to double
            public double RemainingDiskSpace { get; set; }
            public string RightFilamentType { get; set; }
            public double RightTargetTemp { get; set; }
            public double RightTemp { get; set; }
            public string Status { get; set; }
            public int TargetPrintLayer { get; set; }
            public int Tvoc { get; set; }
            public double ZAxisCompensation { get; set; } // technically a float..
        }
    }
}