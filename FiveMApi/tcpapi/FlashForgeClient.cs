using System;
using System.Diagnostics;
using System.Threading.Tasks;
using FiveMApi.api.filament;
using FiveMApi.tcpapi.client;
using FiveMApi.tcpapi.replays;

namespace FiveMApi.tcpapi
{
    public class FlashForgeClient : FlashForgeTcpClient
    { // Legacy API for older printers, but also allows for more control in combination with FiveMClient on newer printers (5m+)
        private readonly GCodeController _control;
        
        public FlashForgeClient(string hostname) : base(hostname)
        {
            _control = new GCodeController(this);
        }

        public string GetIp() { return hostname; }

        internal GCodeController GCode() { return _control; } // helper
        
        // todo check if any other older printers have a runout sensor
        // and if so, make sure the g-code command is the same
        private bool is5mPro;
        
        public async Task<bool> InitControl()
        {
            Debug.WriteLine("(Legacy API) InitControl()");
            var tries = 0;
            while (tries <= 3)
            {
                var result = await SendRawCmd(GCodes.CmdLogin);
                if (!result.Contains("Control failed.") && result.Contains("ok"))
                {
                    await Task.Delay(100);
                    var info = await GetPrinterInfo();
                    if (info == null)
                    {
                        Debug.WriteLine("(Legacy API) Failed to get printer info, aborting.");
                        return false;
                    }
                    Debug.WriteLine("(Legacy API) connected to: " + info.TypeName);
                    Debug.WriteLine("(Legacy API) Firmware version: " + info.FirmwareVersion);
                    if (info.TypeName.Contains("5M") && info.TypeName.Contains("Pro")) is5mPro = true;
                    StartKeepAlive(); // socket "times out" printer-side after a few seconds of no incoming commands..
                    return true;
                }
                tries++;
                // ensures no errors from previous connections that were improperly closed
                await SendRawCmd(GCodes.CmdLogout);
                await Task.Delay(500 * tries);
            }

            return false;
        }

        public async Task<bool> LedOn() { return await _control.LedOn(); }
        
        public async Task<bool> LedOff() { return await _control.LedOff(); }
        
        public async Task<bool> HomeAxes() { return await _control.Home(); }

        public async Task<bool> RapidHome() { return await _control.RapidHome(); }

        public async Task<bool> TurnRunoutSensorOn()
        {
            if (is5mPro) return await SendCmdOk(GCodes.CmdRunoutSensorOn);
            Debug.WriteLine("Filament runout sensor not equipped on this printer.");
            return false;
        }

        public async Task<bool> TurnRunoutSensorOff()
        {
            if (is5mPro) return await SendCmdOk(GCodes.CmdRunoutSensorOff);
            Debug.WriteLine("Filament runout sensor not equipped on this printer.");
            return false;
        }

        public async Task<bool> SetExtruderTemp(int temp, bool waitFor = false) { return await _control.SetExtruderTemp(temp, waitFor); }

        public async Task<bool> CancelExtruderTemp() { return await _control.CancelExtruderTemp(); }

        public async Task<bool> SetBedTemp(int temp, bool waitFor = false) { return await _control.SetBedTemp(temp, waitFor); }

        public async Task<bool> CancelBedTemp(bool waitForCool = false) { return await _control.CancelBedTemp(waitForCool); }

        public async Task<bool> Extrude(int length, int feedrate = 450) { return await SendCmdOk($"~G1 E{length} F{feedrate}"); }

        public async Task<bool> MoveExtruder(int x, int y, int feedrate) { return await SendCmdOk($"~G1 X{x} Y{y} F{feedrate}"); }
        
        public async Task<bool> Move(int x, int y, int z, int feedrate) { return await SendCmdOk($"~G1 X{x} Y{y} Z{z} F{feedrate}"); }
        
        
        // filament load/unload code
        public async Task<bool> PrepareFilamentLoad(Filament filament)
        {
            if (!await CancelExtruderTemp()) return false;
            if (!await SendCmdOk("~G90")) return false; // absolute mode ok
            if (!await HomeAxes()) return false;
            // todo should probably adjust this feedrate for older printers..
            if (!await MoveExtruder(0, 0, 9000)) return false;
            if (!await SetExtruderTemp(filament.LoadTemp, true)) return false; // heat extruder (and wait for it)
            return await Extrude(300); // purge old filament
        }

        private async Task<bool> PrimeNozzle()
        {
            if (await CanExtrude()) return await Extrude(125);
            Debug.WriteLine("PrimeNozzle() failed, nozzle is not hot enough.");
            return false;
        }

        public async Task<bool> LoadFilament()
        {
            if (await CanExtrude()) return await Extrude(250);
            Debug.WriteLine("LoadFilament() failed, nozzle is not hot enough.");
            return false;
        }

        private async Task<bool> CanExtrude()
        {
            var nozzleTemp = await GetNozzleTemp();
            // todo this might need adjustment?
            return nozzleTemp >= 210;
        }

        public async Task<bool> FinishFilamentLoad()
        {
            if (!await CancelExtruderTemp()) return false;
            await Task.Delay(5000);
            //await WaitForExtruderTemp(180);
            return await HomeAxes();
        }

        // base command sending
        internal async Task<bool> SendCmdOk(string cmd)
        {

            try
            {
                var reply = await SendCommandAsync(cmd); // send TCP command
                if (reply.Contains("Received.") && reply.Contains("ok")) return true; // verify response
            }
            catch (Exception ex)
            {  
                Debug.WriteLine($"SendCmdOk exception sending cmd: {cmd} : {ex}");
                return false;
            }
            return false;
        }

        public async Task<string> SendRawCmd(string cmd)
        {
            if (!cmd.Contains("M661")) return await SendCommandAsync(cmd);
            var list = await GetFileListAsync();
            return string.Join("\n", list);
        }
        
        // Replay getters
        public async Task<PrinterInfo> GetPrinterInfo() { return new PrinterInfo().FromReplay(await SendCommandAsync(GCodes.CmdInfoStatus)); }
        public async Task<TempInfo> GetTempInfo() { return new TempInfo().FromReplay(await SendCommandAsync(GCodes.CmdTemp)); }
        public async Task<EndstopStatus> GetEndstopInfo() { return new EndstopStatus().FromReplay(await SendCommandAsync(GCodes.CmdEndstopInfo)); }
        public async Task<PrintStatus> GetPrintStatus() { return new PrintStatus().FromReplay(await SendCommandAsync(GCodes.CmdPrintStatus)); }
        public async Task<LocationInfo> GetLocationInfo() { return new LocationInfo().FromReplay(await SendCommandAsync(GCodes.CmdInfoXyzab)); }

        private async Task<int> GetNozzleTemp()
        {
            var temps = await GetTempInfo();
            return temps.GetExtruderTemp().GetCurrent();
        }
    }
}