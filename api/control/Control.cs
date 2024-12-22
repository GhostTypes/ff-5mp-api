using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FiveMApi.api.filament;
using FiveMApi.api.misc;
using FiveMApi.api.server;
using FiveMApi.tcpapi;
using Newtonsoft.Json;

namespace FiveMApi.api.controls
{
    public class Control
    {
        private readonly FiveMClient _client;
        private readonly FlashForgeClient _tcpClient;
        
        internal Control(FiveMClient client)
        {
            _client = client;
            _tcpClient = client.TcpClient;
        }
        

        public async Task<bool> HomeAxes()
        {
            //if (await _tcpClient.IsSocketBusy()) return false;
            return await _tcpClient.HomeAxes();
        }

        public async Task<bool> HomeAxesRapid()
        {
            //if (await _tcpClient.IsSocketBusy()) return false;
            return await _tcpClient.RapidHome();
        }

        public async Task<bool> SetExternalFiltrationOn()
        {
            if (_client.FiltrationControl) return await SendFiltrationCommand(new FiltrationArgs(false, true));
            Debug.WriteLine("SetExternalFiltrationOn() error , filtration not equipped.");
            return false;
        }

        public async Task<bool> SetInternalFiltrationOn()
        {
            if (_client.FiltrationControl) return await SendFiltrationCommand(new FiltrationArgs(true, false));
            Debug.WriteLine("SetInternalFiltrationOn() error , filtration not equipped.");
            return false;
        }

        public async Task<bool> SetFiltrationOff()
        {
            if (_client.FiltrationControl) return await SendFiltrationCommand(new FiltrationArgs(false, false));
            Debug.WriteLine("SetFiltrationOff() error , filtration not equipped.");
            return false;
        }
        
        // todo not sure if this should hinge on it being the "pro" model
        // but i don't know how 3rd-party camera setups work. It would be great to support those as well
        
        public async Task<bool> TurnCameraOn()
        {
            if (!_client.IsPro) return false;
            return await SendCameraCommand(true);
        }

        public async Task<bool> TurnCameraOff()
        {
            if (!_client.IsPro) return false;
            return await SendCameraCommand(false);
        }
        

        public async Task<bool> SetSpeedOverride(int speed)
        {
            return await SendPrinterControlCmd(printSpeed: speed);
        }

        public async Task<bool> SetZAxisOverride(float offset)
        {
            return await SendPrinterControlCmd(zOffset: offset);
        }

        public async Task<bool> SetChamberFanSpeed(int speed)
        {
            return await SendPrinterControlCmd(chamberFanSpeed: speed);
        }

        public async Task<bool> SetCoolingFanSpeed(int speed)
        {
            return await SendPrinterControlCmd(coolingFanSpeed: speed);
        }

        public async Task<bool> SetLedOn()
        {
            if (_client.LedControl) return await SendControlCommand(Commands.LightControlCmd, new { status = "open" });
            Debug.WriteLine("SetLedOn() error, LEDs not equipped.");
            return false;
        }

        public async Task<bool> SetLedOff()
        {
            if (_client.LedControl) return await SendControlCommand(Commands.LightControlCmd, new { status = "close" });
            Debug.WriteLine("SetLedOff() error, LEDs not equipped.");
            return false;
        }
        
        // todo how do we accurately detect if this is equipped?
        public async Task<bool> TurnRunoutSensorOn()
        {
            return await _tcpClient.TurnRunoutSensorOn();
        }

        public async Task<bool> TurnRunoutSensorOff()
        {
            return await _tcpClient.TurnRunoutSensorOff();
        }
        
        // Filament load/unload/change todo move to its own class
        
        public async Task<bool> PrepareFilamentLoad(Filament filament)
        {
            return await _tcpClient.PrepareFilamentLoad(filament);
        }

        public async Task<bool> LoadFilament()
        {
            return await _tcpClient.LoadFilament();
        }

        public async Task<bool> FinishFilamentLoad()
        {
            return await _tcpClient.FinishFilamentLoad();
        }
        
        
        /// <summary>
        /// Wrapper for sending all /control commands
        /// </summary>
        /// <param name="command">The base command</param>
        /// <param name="argz">Additional arguments</param>
        internal async Task<bool> SendControlCommand(string command, dynamic argz)
        {
            var payload = new
            {
                serialNumber = _client.SerialNumber,
                checkCode = _client.CheckCode,
                payload = new
                {
                    cmd = command,
                    args = argz
                }
            };
            
            
            var settings = new JsonSerializerSettings();
            if (command.Equals(Commands.PrinterControlCmd)) settings.Converters.Add(new ScientificNotationFloatConverter()); // properly serialize data
            
            var jsonPayload = JsonConvert.SerializeObject(payload, settings);
            Debug.WriteLine("SendControlCommand:\n" + jsonPayload);

            try
            {
                await _client.HttpClientSemaphore.WaitAsync();
                var response = await _client.HttpClient.PostAsync(
                    _client.GetEndpoint(Endpoints.Control),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                var data = await response.Content.ReadAsStringAsync();
                Debug.WriteLine($"Command reply: {data}");
                var result = JsonConvert.DeserializeObject<GenericResponse>(data);
                return result.Message.Equals("Success");
            }
            catch (Exception e)
            {
                Console.WriteLine(
                    $"SendControlCommand failure: {command}\n{argz.ToString()}\n{e.Message}\n{e.StackTrace}");
                return false;
            }
            finally
            {
                _client.ReleaseHttpClient();
            }
        }
        
        /// <summary>
        /// Wrapper for sending "printer control" commands
        /// </summary>
        /// <param name="zOffset">Z Offset adjustment</param>
        /// <param name="printSpeed">Print speed offset</param>
        /// <param name="chamberFanSpeed">Chamber Fan Speed</param>
        /// <param name="coolingFanSpeed">Cooling Fan Speed</param>
        /// 
        /// <returns></returns>
        private async Task<bool> SendPrinterControlCmd(float zOffset = 0E0f, int printSpeed = 100, int chamberFanSpeed = 100,
            int coolingFanSpeed = 100)
        {

            var info = await _client.Info.Get();
            
            if (info.CurrentPrintLayer < 2)
            { // don't accidentally turn on the fans in the initial layers
                chamberFanSpeed = 0;
                coolingFanSpeed = 0;
            }
            
            if (!info.IsPrinting()) // will break communication until reboot if sent w/o an active job.. nice one FlashForge.
                throw new Exception("Attempted to send printerCtl_cmd with no active job");
            
            
            var payload = new
            {
                zAxisCompensation = zOffset,
                speed = printSpeed,
                chamberFan = chamberFanSpeed,
                coolingFan = coolingFanSpeed,
                coolingLeftFan = 0 // this is unused? not sure why this is in the firmware.
            };

            return await SendControlCommand(Commands.PrinterControlCmd, payload);
        }

        internal async Task<bool> SendJobControlCmd(string command)
        {
            var payload = new
            {
                jobID = "",
                action = command
            };
            return await SendControlCommand(Commands.JobControlCmd, payload);
        }
        
        public class GenericResponse
        {
            public int Code { get; set; }
            public string Message { get; set; }
        }
        
        public class FiltrationArgs
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
            return await SendControlCommand(Commands.CirculationControlCmd, argz);
        }

        private async Task<bool> SendCameraCommand(bool enabled)
        {
            var payload = new { action = enabled ? "open" : "close" };
            return await SendControlCommand(Commands.CameraControlCmd, payload);
        }
    }
}