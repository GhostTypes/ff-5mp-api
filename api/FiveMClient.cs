using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace FiveMApi.api
{
    public class FiveMClient
    {
        private readonly string _ipAddress;
        private readonly string _serialNumber;
        private readonly string _checkCode;
        private const int Port = 8898;
        private readonly HttpClient _client;

        private const string DetailEndpoint = "/detail";
        private const string ControlEndpoint = "/control";
        private const string UploadFileEndpoint = "/uploadGcode";

        private const string LightControlCmd = "lightControl_cmd";
        private const string PrinterControlCmd = "printerCtl_cmd";
        private const string CirculationControlCmd = "circulateCtl_cmd";

        public FiveMClient(string ipAddress, string serialNumber, string checkCode)
        {
            _ipAddress = ipAddress;
            _serialNumber = serialNumber;
            _checkCode = checkCode;

            _client = new HttpClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
        }

        private string GetEndpoint(string endpoint)
        {
            return $"http://{_ipAddress}:{Port}{endpoint}";
        }

        public async Task<MachineInfo> GetMachineInfo()
        {
            var ds = await GetDetails();
            return ds == null ? null : new MachineInfo().FromDetail(ds.Detail);
        }

        private async Task<DetailResponse> GetDetails()
        {
            var payload = new
            {
                serialNumber = _serialNumber,
                checkCode = _checkCode
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            try
            {
                var response = await _client.PostAsync(GetEndpoint(DetailEndpoint),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode(); // Throws if not 2XX
                var data = await response.Content.ReadAsStringAsync();

                // Log the raw JSON response for debugging
                //Console.WriteLine($"Raw response: {data}");

                return JsonConvert.DeserializeObject<DetailResponse>(data);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine($"Request error: {e.Message}");
                if (e.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {e.InnerException.Message}");
                }
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Unexpected error: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Wrapper for sending all /control commands
        /// </summary>
        /// <param name="command">The base command</param>
        /// <param name="argz">Additional arguments</param>
        private async Task<bool> SendControlCommand(string command, dynamic argz)
        {
            var payload = new
            {
                serialNumber = _serialNumber,
                checkCode = _checkCode,
                payload = new
                {
                    cmd = command,
                    args = argz
                }
            };
            var jsonPayload = JsonConvert.SerializeObject(payload);

            Console.WriteLine("SendControlCommand:\n" + jsonPayload);

            try
            {
                var response = await _client.PostAsync(GetEndpoint(ControlEndpoint),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GenericResponse>(data);
                return result.Message.Equals("Success");
            }
            catch (Exception e)
            {
                Console.WriteLine($"SendControlCommand failure: {command}\n{argz.ToString()}\n{e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Wrapper for sending "printer control" commands
        /// </summary>
        /// <param name="zOffset">Z Offset adjustment</param>
        /// <param name="printSpeed">Print speed offset</param>
        /// <param name="chamberFanSpeed">Chamber Fan Speed</param>
        /// <param name="coolingFanSpeed">Cooling Fan Speed</param>
        /// <returns></returns>
        private async Task<bool> SendPrinterControlCmd(string zOffset = "0E0", int printSpeed = 100, int chamberFanSpeed = 100,
            int coolingFanSpeed = 100)
        {
            var payload = new
            {
                zAxisCompensation = zOffset,
                speed = printSpeed,
                chamberFan = chamberFanSpeed,
                coolingFan = coolingFanSpeed
            };

            return await SendControlCommand(PrinterControlCmd, payload);
        }

        /// <summary>
        /// Upload a GCode/3MF file to the printer
        /// </summary>
        /// <param name="filePath">Path to the file to upload</param>
        /// <param name="startPrint">Start the printer after uploading</param>
        /// <param name="levelBeforePrint">Level the bed before printing</param>
        public async Task<bool> UploadFile(string filePath, bool startPrint, bool levelBeforePrint)
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            using (var content = new MultipartFormDataContent("------------------------DHD3lr8XwXBuyC8G3dWjK7"))
            {
                content.Headers.Add("serialNumber", _serialNumber);
                content.Headers.Add("checkCode", _checkCode);
                content.Headers.Add("fileSize", fileSize.ToString());
                content.Headers.Add("printNow", startPrint.ToString().ToLower());
                content.Headers.Add("levelingBeforePrint", levelBeforePrint.ToString().ToLower());

                var fileContent = new StreamContent(fileInfo.OpenRead());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"gcodeFile\"",
                    FileName = "\"" + fileInfo.Name + "\""
                };
                content.Add(fileContent);

                _client.DefaultRequestHeaders.ExpectContinue = true;
                try
                {
                    var response = await _client.PostAsync(GetEndpoint(UploadFileEndpoint), content);
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<GenericResponse>(data);
                    _client.DefaultRequestHeaders.ExpectContinue = false;
                    return result.Message.Equals("Success");
                }
                catch (Exception e)
                {
                    _client.DefaultRequestHeaders.ExpectContinue = false;
                    Console.WriteLine($"UploadFile error: {e.Message}\n{e.StackTrace}");
                    return false;
                }
            }
        }

        // Filtration commands
        private class FiltrationArgs
        {
            [JsonProperty("internal")]
            public string Internal { get; set; }
            [JsonProperty("external")]
            public string External { get; set; }

            public FiltrationArgs(bool i, bool e)
            {
                Internal = i ? "open" : "close";
                External = e ? "open" : "close";
            }
        }

        private async Task<bool> SendFiltrationCommand(FiltrationArgs argz)
        {
            return await SendControlCommand(CirculationControlCmd, argz);
        }

        /// <summary>
        /// Turn external filtration on (disables internal)
        /// </summary>
        public async Task<bool> SetExternalFiltrationOn()
        {
            return await SendFiltrationCommand(new FiltrationArgs(false, true));
        }
        /// <summary>
        /// Turn internal filtration on (disables external)
        /// </summary>
        public async Task<bool> SetInternalFiltrationOn()
        {
            return await SendFiltrationCommand(new FiltrationArgs(true, false));
        }
        /// <summary>
        /// Turn internal/external filtration off
        /// </summary>
        public async Task<bool> SetFiltrationOff()
        {
            return await SendFiltrationCommand(new FiltrationArgs(false, false));
        }

        // sending speed (overrides)
        public async Task<bool> SetSpeedOverride(int speed)
        {
            return await SendPrinterControlCmd(printSpeed: speed);
        }

        public async Task<bool> SetChamberFanSpeed(int speed)
        {
            return await SendPrinterControlCmd(chamberFanSpeed: speed);
        }

        public async Task<bool> SetCoolingFanSpeed(int speed)
        {
            return await SendPrinterControlCmd(coolingFanSpeed: speed);
        }

        // led control
        public async Task<bool> SetLedOn()
        {
            var payload = new
            {
                status = "open"
            };
            return await SendControlCommand(LightControlCmd, payload);
        }

        public async Task<bool> SetLedOff()
        {
            var payload = new
            {
                status = "close"
            };
            return await SendControlCommand(LightControlCmd, payload);
        }

        public async Task<bool> VerifyConnection()
        {
            var details = await GetDetails();
            return details != null && details.Message.Equals("Success");
        }

        public class MachineInfo
        {
            // translate flashforge response (DetailResponse) into something that's easier to use
            // also drops some unnecessary values
            public bool AutoShutdown { get; set; }
            public int AutoShutdownTime { get; set; }

            public string CameraStreamUrl { get; set; }

            // Fan speeds
            public int ChamberFanSpeed { get; set; }
            public int CoolingFanSpeed { get; set; }

            /// <summary>
            /// Lifetime filament used (in meters)
            /// </summary>
            public double CumulativeFilament { get; set; }
            /// <summary>
            /// Lifetime print time
            /// </summary>
            public int CumulativePrintTime { get; set; }

            public int CurrentPrintSpeed { get; set; }

            public bool DoorOpen { get; set; }
            public string ErrorCode { get; set; }

            /// <summary>
            /// Estimated filament used in meters
            /// </summary>
            public double EstLength { get; set; }
            /// <summary>
            /// Estimated filament used in grams
            /// </summary>
            public double EstWeight { get; set; }

            public double EstimatedTime { get; set; }

            // Fans & Led Status
            public bool ExternalFanOn { get; set; }
            public bool InternalFanOn { get; set; }
            public bool LightsOn { get; set; }

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
            /// Url for thumbnail of the current job
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

            // extras
            public string PrintEta { get; set; }
            public string FormattedRunTime { get; set; }
            public string FormattedTotalRunTime { get; set; }

            public MachineInfo FromDetail(Detail detail)
            {
                PrintEta = TimeSpan.FromSeconds(detail.EstimatedTime).ToString(@"hh\:mm");
                FormattedRunTime = TimeSpan.FromSeconds(detail.PrintDuration).ToString(@"hh\:mm");
                FormattedTotalRunTime = TimeSpan.FromSeconds(detail.CumulativePrintTime).ToString(@"hh\:mm");

                // convert open/closed string to true/false
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

                EstLength = detail.EstimatedRightLen;
                EstWeight = detail.EstimatedRightWeight;
                EstimatedTime = detail.EstimatedTime;

                FillAmount = detail.FillAmount;
                FirmwareVersion = detail.FirmwareVersion;
                Name = detail.Name;
                NozzleSize = detail.NozzleModel;

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

                return this;
            }
        }

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
            public string DoorStatus { get; set; }
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
            public double ZAxisCompensation { get; set; }
        }

        private class DetailResponse
        {
            public int Code { get; set; }
            public Detail Detail { get; set; }
            public string Message { get; set; }
        }

        private class GenericResponse
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }
    }
}
