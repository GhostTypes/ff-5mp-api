using System.Threading.Tasks;
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

        public async Task<bool> SetExtruderTemp(int temp)
        {
            return await _tcpClient.SetExtruderTemp(temp);
        }

        public async Task<bool> SetBedTemp(int temp)
        {
            return await _tcpClient.SetBedTemp(temp);
        }

        public async Task<bool> CancelExtruderTemp()
        {
            return await _tcpClient.CancelExtruderTemp();
        }

        public async Task<bool> CancelBedTemp()
        {
            return await _tcpClient.CancelBedTemp();
        }

        public async Task WaitForPartCool(int temp)
        {
            while (true)
            {
                // rather get just the temp info than use the new api and extract just the temp info
                var temps = await _tcpClient.GetTempInfo();
                if (temps.IsCooled()) break;
            }
        }
    }
}