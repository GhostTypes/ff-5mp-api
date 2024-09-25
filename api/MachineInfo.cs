using System;

namespace FiveMApi.api
{
    public partial class FiveMClient
    {
        public class MachineInfo
        {
            // Translates flashforge response (DetailResponse) into something that's easier to use
            // Also drops some unnecessary values

            /// <summary>
            /// If auto shutdown is enabled
            /// </summary>
            public bool AutoShutdown { get; set; }
            /// <summary>
            /// The auto-shutdown time (always 30?)
            /// </summary>
            public int AutoShutdownTime { get; set; }

            /// <summary>
            /// URL to the printer webcam's MJPEG stream
            /// </summary>
            public string CameraStreamUrl { get; set; }

            // Fan speeds
            /// <summary>
            /// Chamber fan speed
            /// </summary>
            public int ChamberFanSpeed { get; set; }
            /// <summary>
            /// Cooling fan speed
            /// </summary>
            public int CoolingFanSpeed { get; set; }

            /// <summary>
            /// Lifetime filament used in meters
            /// </summary>
            public double CumulativeFilament { get; set; }
            /// <summary>
            /// Lifetime print time in minutes
            /// </summary>
            public int CumulativePrintTime { get; set; }

            /// <summary>
            /// Current print speed in mm/s
            /// </summary>
            public int CurrentPrintSpeed { get; set; }

            /// <summary>
            /// Remaining disk space in GB, returned like 1.23 
            /// </summary>
            public string FreeDiskSpace { get; set; }

            /// <summary>
            /// Printer door status, seems broken in firmware 2.7.5
            /// </summary>
            public bool DoorOpen { get; set; }
            /// <summary>
            /// Current error code, if any
            /// </summary>
            public string ErrorCode { get; set; }

            /// <summary>
            /// Estimated filament used so far in meters (current print)
            /// </summary>
            public double EstLength { get; set; }
            /// <summary>
            /// Estimated filament used so far in grams (current print)
            /// </summary>
            public double EstWeight { get; set; }

            public double EstimatedTime { get; set; }

            // Fans & LED Status
            public bool ExternalFanOn { get; set; }
            public bool InternalFanOn { get; set; }
            public bool LightsOn { get; set; }

            // Network
            public string IpAddress { get; set; }
            public string MacAddress { get; set; }

            /// <summary>
            /// Current print infill amount
            /// </summary>
            public int FillAmount { get; set; }
            public string FirmwareVersion { get; set; }

            public string Name { get; set; } // Adventurer 5M Pro
            public string NozzleSize { get; set; } // 0.4mm etc

            // Print Bed and Extruder Temp
            public double PrintBedTemp { get; set; }
            public double PrintBedSetTemp { get; set; }

            public double ExtruderTemp { get; set; }
            public double ExtruderSetTemp { get; set; }

            // Current Print Stats

            /// <summary>
            /// Elapsed print time in seconds
            /// </summary>
            public int PrintDuration { get; set; }
            /// <summary>
            /// Name of the current job
            /// </summary>
            public string PrintFileName { get; set; }
            /// <summary>
            /// URL for thumbnail of the current job
            /// </summary>
            public string PrintFileThumbUrl { get; set; }
            public int CurrentPrintLayer { get; set; }

            /// <summary>
            /// Print progress as a double, from 0.X - 1.0
            /// </summary>
            public double PrintProgress { get; set; }

            /// <summary>
            /// Print progress as an int, from 0 - 100
            /// </summary>
            public int PrintProgressInt { get; set; }

            public double PrintSpeedAdjust { get; set; } // Changed from int to double
            public string FilamentType { get; set; }

            public string Status { get; set; }
            public int TotalPrintLayers { get; set; }
            public int Tvoc { get; set; }
            public double ZAxisCompensation { get; set; }

            // Cloud codes
            public string FlashCloudRegisterCode { get; set; }
            public string PolarCloudRegisterCode { get; set; }

            // Extras
            /// <summary>
            /// Estimated time remaining for current job in hh:mm
            /// </summary>
            public string PrintEta { get; set; }
            /// <summary>
            /// Current job run time in hh:mm
            /// </summary>
            public string FormattedRunTime { get; set; }
            /// <summary>
            /// Lifetime run time in hh:mm
            /// </summary>
            public string FormattedTotalRunTime { get; set; }

            public bool IsPrinting()
            {
                return Status.Equals("printing");
            }

            public MachineInfo FromDetail(Detail detail)
            {
                PrintEta = TimeSpan.FromSeconds(detail.EstimatedTime).ToString(@"hh\:mm");
                FormattedRunTime = TimeSpan.FromSeconds(detail.PrintDuration).ToString(@"hh\:mm");

                var totalMinutes = detail.CumulativePrintTime;
                var hours = totalMinutes / 60;
                var minutes = totalMinutes % 60;

                FormattedTotalRunTime = $"{hours}h:{minutes}m";

                // Convert open/closed string to true/false
                AutoShutdown = detail.AutoShutdown.Equals("open");
                DoorOpen = detail.DoorStatus.Equals("open");
                ExternalFanOn = detail.ExternalFanStatus.Equals("open");
                InternalFanOn = detail.InternalFanStatus.Equals("open");
                LightsOn = detail.LightStatus.Equals("open");

                AutoShutdownTime = detail.AutoShutdownTime;
                CameraStreamUrl = detail.CameraStreamUrl;
                ChamberFanSpeed = detail.ChamberFanSpeed;
                CoolingFanSpeed = detail.CoolingFanSpeed;
                CumulativeFilament = detail.CumulativeFilament;
                CumulativePrintTime = detail.CumulativePrintTime;
                CurrentPrintSpeed = detail.CurrentPrintSpeed;
                ErrorCode = detail.ErrorCode;

                // Adjusted calculation for EstLength and EstWeight
                // Convert total estimated filament from millimeters to meters
                double totalJobFilamentMeters = detail.EstimatedRightLen / 1000.0;

                // Calculate filament used so far based on print progress
                double filamentUsedSoFarMeters = totalJobFilamentMeters * detail.PrintProgress;

                EstLength = filamentUsedSoFarMeters;

                // Calculate estimated filament weight used so far in grams
                EstWeight = detail.EstimatedRightWeight * detail.PrintProgress;

                // Debug output (optional)
                //Console.WriteLine($"Total job filament (meters): {totalJobFilamentMeters}");
                //Console.WriteLine($"Filament used so far (meters): {EstLength}");
                //Console.WriteLine($"Print progress: {detail.PrintProgress * 100}%");

                EstimatedTime = detail.EstimatedTime;

                FillAmount = detail.FillAmount;
                FirmwareVersion = detail.FirmwareVersion;
                FreeDiskSpace = detail.RemainingDiskSpace.ToString("F2"); // e.g., 4.99
                Name = detail.Name;
                NozzleSize = detail.NozzleModel;

                MacAddress = detail.MacAddr;
                IpAddress = detail.IpAddr;

                PrintBedTemp = detail.PlatTemp;
                PrintBedSetTemp = detail.PlatTargetTemp;

                PrintDuration = detail.PrintDuration;
                PrintFileName = detail.PrintFileName;
                PrintFileThumbUrl = detail.PrintFileThumbUrl;

                CurrentPrintLayer = detail.PrintLayer;
                PrintProgress = detail.PrintProgress;
                PrintProgressInt = (int)(PrintProgress * 100);

                PrintSpeedAdjust = detail.PrintSpeedAdjust;

                FilamentType = detail.RightFilamentType;
                ExtruderTemp = detail.RightTemp;
                ExtruderSetTemp = detail.RightTargetTemp;

                Status = detail.Status;
                TotalPrintLayers = detail.TargetPrintLayer;
                Tvoc = detail.Tvoc;
                ZAxisCompensation = detail.ZAxisCompensation;

                FlashCloudRegisterCode = detail.FlashRegisterCode;
                PolarCloudRegisterCode = detail.PolarRegisterCode;

                return this;
            }
        }
    }
}
