using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace FiveMApi.tcpapi.client
{
    public class GCodeController
    {

        private readonly FlashForgeClient tcpClient;
        
        internal GCodeController(FlashForgeClient tcpClient)
        {
            this.tcpClient = tcpClient;
        }
        
        // legacy / custom led control
        public async Task<bool> LedOn()
        {
            return await tcpClient.SendCmdOk(GCodes.CmdLedOn);
        }

        public async Task<bool> LedOff()
        {
            return await tcpClient.SendCmdOk(GCodes.CmdLedOff);
        }
        
        
        // movement
        public async Task<bool> Home()
        {
            return await tcpClient.SendCmdOk(GCodes.CmdHomeAxes);
        }
        
        public async Task<bool> RapidHome()
        {
            if (!await tcpClient.SendCmdOk("~G90")) return false;
            if (!await Move(105, 105, 220, 9000)) return false;
            return await Home();
        }
        
        public async Task<bool> Move(int x, int y, int z, int feedrate)
        {
            return await tcpClient.SendCmdOk($"~G1 X{x} Y{y} Z{z} F{feedrate}");
        }
        
        public async Task<bool> MoveExtruder(int x, int y, int feedrate)
        {
            return await tcpClient.SendCmdOk($"~G1 X{x} Y{y} F{feedrate}");
        }
        
        public async Task<bool> Extrude(int length, int feedrate = 450)
        {
            return await tcpClient.SendCmdOk($"~G1 E{length} F{feedrate}");
        }
        
        // temps
        public async Task<bool> SetExtruderTemp(int temp, bool waitFor = false)
        {
            var ok = await tcpClient.SendCmdOk($"~M104 S{temp}");
            if (!waitFor) return ok;
            return await WaitForExtruderTemp(temp);
        }
        
        public async Task<bool> SetBedTemp(int temp, bool waitFor = false)
        {
            var ok = await tcpClient.SendCmdOk($"~M140 S{temp}");
            if (!waitFor) return ok;
            return await WaitForBedTemp(temp);
        }

        public async Task<bool> CancelExtruderTemp()
        {
            // todo impl wait?
            return await tcpClient.SendCmdOk("~M104 S0");
        }
        
        public async Task<bool> CancelBedTemp(bool waitForCool = false)
        {
            var ok = await tcpClient.SendCmdOk("~M140 S0");
            if (!waitForCool) return ok;
            return await WaitForBedTemp(37); // *can* remove parts @ 40 but safer side
        }
        
        public async Task<bool> WaitForBedTemp(int temp)
        {
            var cts = new CancellationTokenSource(30000); // 30s timeout
            while (!cts.Token.IsCancellationRequested)
            {
                var tempInfo = await tcpClient.GetTempInfo();
                if (tempInfo.GetBedTemp().GetCurrent().Equals(temp)) return true;
            }
            Debug.WriteLine($"WaitForBedTemp (target {temp}) timed out after 30s.");
            return false;
        }
        
        public async Task<bool> WaitForExtruderTemp(int temp)
        {
            var cts = new CancellationTokenSource(30000); // 30s timeout
            while (!cts.Token.IsCancellationRequested)
            {
                var tempInfo = await tcpClient.GetTempInfo();
                if (tempInfo.GetExtruderTemp().GetCurrent().Equals(temp)) return true;
            }
            Debug.WriteLine($"WaitForExtruderTemp (target {temp}) timed out after 30s.");
            return false;
        }
    }
}