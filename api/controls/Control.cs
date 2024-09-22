using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FiveMApi.api.server;
using Newtonsoft.Json;

namespace FiveMApi.api.controls
{
    public class Control
    {
        private readonly FiveMClient _printerClient;
        
        
        private const string LightControlCmd = "lightControl_cmd";
        private const string PrinterControlCmd = "printerCtl_cmd";
        private const string CirculationControlCmd = "circulateCtl_cmd";
        private const string JobControlCmd = "jobCtl_cmd";

        internal Control(FiveMClient printerClient)
        {
            _printerClient = printerClient;
        }
        

        public async Task<bool> HomeAxes()
        {
            return await _printerClient.TcpClient.HomeAxes();
        }

        public async Task<bool> SetExternalFiltrationOn()
        {
            return await SendFiltrationCommand(new FiltrationArgs(false, true));
        }

        public async Task<bool> SetInternalFiltrationOn()
        {
            return await SendFiltrationCommand(new FiltrationArgs(true, false));
        }

        public async Task<bool> SetFiltrationOff()
        {
            return await SendFiltrationCommand(new FiltrationArgs(false, false));
        }

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
        
        
        /// <summary>
        /// Wrapper for sending all /control commands
        /// </summary>
        /// <param name="command">The base command</param>
        /// <param name="argz">Additional arguments</param>
        internal async Task<bool> SendControlCommand(string command, dynamic argz)
        {
            var payload = new
            {
                serialNumber = _printerClient.SerialNumber,
                checkCode = _printerClient.CheckCode,
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
                var response = await _printerClient.HttpClient.PostAsync(_printerClient.GetEndpoint(Endpoints.Control),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                var data = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Command reply: {data}");
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

        internal async Task<bool> SendJobControlCmd(string command)
        {
            var payload = new
            {
                jobID = "",
                action = command
            };
            return await SendControlCommand(JobControlCmd, payload);
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
            return await SendControlCommand(CirculationControlCmd, argz);
        }
    }
}