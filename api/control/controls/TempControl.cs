using System.Threading.Tasks;
using FiveMApi.api.server;
using FiveMApi.tcpapi;

namespace FiveMApi.api.controls
{
    public class TempControl
    {
        
        private readonly FiveMClient _printerClient;
        private readonly FlashForgeClient _tcpClient;
        
        internal TempControl(FiveMClient printerClient)
        {
            _printerClient = printerClient;
            _tcpClient = printerClient.TcpClient;
        }

        public async Task<bool> SetExtruderTemp(int temp) { return await _tcpClient.SetExtruderTemp(temp); }

        public async Task<bool> SetBedTemp(int temp) { return await _tcpClient.SetBedTemp(temp); }

        public async Task<bool> CancelExtruderTemp() { return await _tcpClient.CancelExtruderTemp(); }

        public async Task<bool> CancelBedTemp() { return await _tcpClient.CancelBedTemp(); }

        public async Task WaitForPartCool(int temp)
        {
            await _tcpClient.GCode().WaitForBedTemp(temp);
        }
        
        // todo this *should* work as is but I can't find any reference to it's use in FlashForge src
        // There is a fair bit of code in FlashNetwork but only a small amount is actually implemented (in slicers or in the library itself)
        // So until it breaks I'll continue to use the TCP api for temp control and other "lower level" controls
        
        /*private async Task<bool> SendTempControlCommand(double bedTemp, double rightExtruder, double leftExtruder,
            double chamberTemp)
        {
            var payload = new
            {
                platformTemp = bedTemp,
                rightTemp = rightExtruder,
                leftTemp = leftExtruder,
                chamberTemp = chamberTemp
            };

            return await _printerClient.Control.SendControlCommand(Commands.TempControlCmd, payload);
        }*/
        
        
        
    }
}