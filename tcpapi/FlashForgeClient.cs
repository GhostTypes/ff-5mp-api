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

        //private const string CmdPrintStatus = "~M27";
        //private const string CmdEndstopInfo = "~M119";
        private const string CmdInfoStatus = "~M115";
        //private const string CmdInfoXyzab = "~M114";
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
                if (!result.Contains("Control failed.") && result.Contains("ok")) return true;
                tries++;
            }

            return false;
        }
        
        public async Task<bool> HomeAxes()
        {
            return await SendCmdOk(CmdHomeAxes);
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
        
    
        // All of this is technically "legacy code" - for the api used with Flashprint
        // This was mainly updated as the new api(s) (for use with Orca-FlashForge)
        // don't seem to support sending G-Code commands, which is.. important lol
        
        
        //public async Task<bool> TurnLightsOff()
        //{
        //    return await SendCmdOk(CmdLedOff);
        //}

        //public async Task<bool> TurnLightsOn()
        //{
        //    return await SendCmdOk(CmdLedOn);
        //}
        
        /**
         * Transfer & saves a gcode file to the printer, and starts printing it
         */
        /**public async Task<bool> StartPrint(string file)
        {
            if (!File.Exists(file)) return false; // this should never happen
            if (!await TransferFile(file)) return false;
            return await SendStartPrint(Path.GetFileName(file));
        }**/

        /**
         * Transfers a gcode file to the printer's local storage
         */
        /**public async Task<bool> TransferFile(string file)
        {
            if (!File.Exists(file)) return false; // this should never happen
            var name = Path.GetFileName(file);
            try
            {
                var data = File.ReadAllBytes(file);
                if (!await InitFileTransfer(name, data.Length)) return false;
                var gcode = Utils.PrepareRawData(data);
                if (!await SendRawDataAsync(gcode)) return false;
                return await CompleteFileTransfer();
            } catch (IOException ioEx)
            {
                Debug.WriteLine("Unable to start print with file (IOException) : " + file);
                Debug.WriteLine(ioEx.StackTrace);
                return false;
            }
        }**/

        /**private async Task<bool> InitFileTransfer(string name, int length)
        {
            return await SendCmdOk(CmdPrintStart
                .Replace("%%size%%", length.ToString()
                .Replace("%%filename%%", name)));
        }**/

        /**private async Task<bool> CompleteFileTransfer()
        {
            return await SendCmdOk(CmdSaveFile);
        }

        private async Task<bool> SendStartPrint(string filename)
        {
            return await SendCmdOk(CmdPrintStart.Replace("%%filename%%", filename));
        }**/
        
        
        private async Task<bool> SendCmdOk(string cmd)
        {

            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    var reply = await SendCommandAsync(cmd); // send TCP command
                    if (reply.Contains("Received.") && reply.Contains("ok")) return true; // verify response
                }
                catch (Exception ex)
                {  
                    Console.WriteLine($"Exception sending cmd: {cmd} : {ex}");
                    continue;
                }
                
                Console.WriteLine($"SendCmdOk failed with cmd: {cmd} (attempt {attempt})");
                await InitControl(); // make sure control is set
                await Task.Delay(500 * attempt); 
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
        

        //public async Task<EndstopStatus> GetEndstopInfo()
        //{
       //     return new EndstopStatus().FromReplay(await SendCommandAsync(CmdEndstopInfo));
        //}

        //public async Task<PrintStatus> GetPrintStatus()
        //{
        //    return new PrintStatus().FromReplay(await SendCommandAsync(CmdPrintStatus));
        //}

        //public async Task<LocationInfo> GetLocationInfo()
        //{
        //    return new LocationInfo().FromReplay(await SendCommandAsync(CmdInfoXyzab));
        //}

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