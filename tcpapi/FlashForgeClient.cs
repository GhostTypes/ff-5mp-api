using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FiveMApi.api.filament;
using FiveMApi.tcpapi.replays;

namespace FiveMApi.tcpapi
{
    public class FlashForgeClient : FlashForgeTcpClient
    {
        public readonly string CmdLogin = "~M601 S1";
        public readonly string CmdLogout = "~M602";

        private const string CmdPrintStatus = "~M27";
        private const string CmdEndstopInfo = "~M119";
        private const string CmdInfoStatus = "~M115";
        private const string CmdInfoXyzab = "~M114";
        private const string CmdTemp = "~M105";

        //private const string CmdLedOn = "~M146 r255 g255 b255 F0";
        //private const string CmdLedOff = "~M146 r0 g0 b0 F0";

        //private const string CmdPrintStart = "~M23 0:/user/%%filename%%\r";
        //private const string CmdPrintStop = "~M26";

        //public readonly string CmdStartTransfer = "~M28 %%size%% 0:/user/%%filename%%\r";
        //private const string CmdSaveFile = "~M29\r";

        private const string CmdRunoutSensorOn = "~M405";
        private const string CmdRunoutSensorOff = "~M406";

        private const string CmdListLocalFiles = "~M661";
        private const string CmdGetThumbnail = "~M662";

        private const string TakePicture = "~M240";

        private const string CmdHomeAxes = "~G28";
        
        
        
        public FlashForgeClient(string hostname) : base(hostname)
        {
            
        }

        public string GetIp()
        {
            return hostname;
        }
        
        public async void Shutdown()
        {
            await SendRawCmd(CmdLogout);
            Dispose();
        }

        
        
        public async Task<bool> InitControl()
        {
            //await SendCommandAsync(CmdLogin);
            var tries = 0;
            while (tries <= 5)
            {
                var result = await SendRawCmd(CmdLogin);
                if (!result.Contains("Control failed.") && result.Contains("ok"))
                {
                    StartKeepAlive(); // socket "times out" printer-side after a few seconds of no incoming commands..
                    return true;
                }
                tries++;
                // ensures no errors from previous connections that were improperly closed
                await SendRawCmd(CmdLogout);
                await Task.Delay(500);
            }

            return false;
        }
        
        public async Task<bool> HomeAxes()
        {
            try
            {
                await SendRawCmd(CmdHomeAxes);
                return true;
            }
            catch (Exception ignored)
            {
                return false;
            }
        }

        public async Task<bool> RapidHome()
        {
            if (!await SendCmdOk("~G90")) return false;
            if (!await SendCmdOk("~G1 X105 ZY105 Z220 F9000")) return false;
            return await HomeAxes();
        }

        public async Task<bool> TurnRunoutSensorOn()
        {
            return await SendCmdOk(CmdRunoutSensorOn);
        }

        public async Task<bool> TurnRunoutSensorOff()
        {
            return await SendCmdOk(CmdRunoutSensorOff);
        }

        public async Task<bool> SetExtruderTemp(int temp)
        {
            return await SendCmdOk($"~M104 S{temp}");
        }

        public async Task<bool> CancelExtruderTemp()
        {
            return await SendCmdOk("~M104 S0");
        }

        public async Task<bool> SetBedTemp(int temp)
        {
            return await SendCmdOk($"~M140 S{temp}");
        }

        public async Task<bool> CancelBedTemp()
        {
            return await SendCmdOk("~M140 S0");
        }

        public async Task<bool> WaitForBedTemp(int temp)
        {
            return await SendCmdOk($"~M190 S{temp}"); // todo make sure this works as expected
            // it might just return ok and wait printer-side..
        }
        
        
        // filament load/unload code
        public async Task<bool> PrepareFilamentLoad(Filament filament)
        {
            if (!await CancelExtruderTemp()) return false;
            if (!await SendCmdOk("~G90")) return false; // absolute mode
            if (!await HomeAxes()) return false;
            if (!await SendCmdOk("~G1 X0 Y0 F9000")) return false; // move extruder to the middle
            if (!await SetExtruderTemp(filament.LoadTemp)) return false; // heat extruder
            return await SendCmdOk("~G1 E25 F450"); // purge old filament
        }

        public async Task<bool> LoadFilament()
        {
            return await SendCmdOk("~G1 E125 F450");
        }

        public async Task<bool> FinishFilamentLoad()
        {
            await Task.Delay(5); // let nozzle cool a bit
            if (!await CancelExtruderTemp()) return false;
            return await HomeAxes();
        }
        
        
        private async Task<bool> SendCmdOk(string cmd)
        {

            try
            {
                var reply = await SendCommandAsync(cmd); // send TCP command
                if (reply.Contains("Received.") && reply.Contains("ok")) return true; // verify response
            }
            catch (Exception ex)
            {  
                Console.WriteLine($"Exception sending cmd: {cmd} : {ex}");
                return false;
            }
            return false;
        }

        public async Task<string> SendRawCmd(string cmd)
        {
            var result = await SendRawGCodeCmd(cmd);
            return result;
        }
        
        private async Task<string> SendRawGCodeCmd(string cmd)
        {
            if (!cmd.Contains("M661")) return await SendCommandAsync(cmd);
            var list = await GetFileListAsync();
            return string.Join("\n", list);
        }
        

        public async Task<PrinterInfo> GetPrinterInfo()
        {
            return new PrinterInfo().FromReplay(await SendCommandAsync(CmdInfoStatus));
        }

        public async Task<TempInfo> GetTempInfo()
        {
            return new TempInfo().FromReplay(await SendCommandAsync(CmdTemp));
        }
        

        public async Task<EndstopStatus> GetEndstopInfo()
        {
            return new EndstopStatus().FromReplay(await SendCommandAsync(CmdEndstopInfo));
        }

        public async Task<PrintStatus> GetPrintStatus()
        {
            return new PrintStatus().FromReplay(await SendCommandAsync(CmdPrintStatus));
        }

        public async Task<LocationInfo> GetLocationInfo()
        {
            return new LocationInfo().FromReplay(await SendCommandAsync(CmdInfoXyzab));
        }

        public async Task<bool> Validate()
        {
            var info = await GetPrinterInfo();
            return info != null;
        }

        
        //public async void StopPrinting()
        //{
        //    await SendCommandAsync(CmdPrintStop);
        //}
    }
}