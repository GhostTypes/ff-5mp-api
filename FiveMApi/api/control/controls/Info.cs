using System;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FiveMApi.api.server;
using Newtonsoft.Json;

namespace FiveMApi.api.control.controls
{
    public class Info
    {
        
        private readonly FiveMClient _client;
        
        internal Info(FiveMClient printerClient)
        {
            _client = printerClient;
        }

        public class DetailResponse
        {
            public int Code { get; set; }
            public FiveMClient.Detail Detail { get; set; }
            public string Message { get; set; }
        }

        public async Task<FiveMClient.MachineInfo> Get()
        {
            var detail = await GetDetailResponse();
            return detail == null ? null : new FiveMClient.MachineInfo().FromDetail(detail.Detail);
        }

        public async Task<bool> IsPrinting()
        {
            var info = await Get();
            return info.Status == "printing";
        }
        

        public async Task<string> GetStatus()
        {
            var info = await Get();
            return info.Status;
        }

        public async Task<MachineState> GetState()
        {
            var info = await Get();
            return info.MachineState;
        }
        
        public async Task<DetailResponse> GetDetailResponse()
        {
            var payload = new
            {
                serialNumber = _client.SerialNumber,
                checkCode = _client.CheckCode
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            try
            {
                var response = await _client.HttpClient.PostAsync(
                    _client.GetEndpoint(Endpoints.Detail),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<DetailResponse>(data);
            }
            catch (HttpRequestException e)
            {
                Debug.WriteLine($"GetDetailResponse Request error: {e.Message}");
                if (e.InnerException != null)
                {
                    Debug.WriteLine($"GetDetailResponse Inner exception: {e.InnerException.Message}");
                }
                return null;
            }
            catch (Exception e)
            {
                Debug.WriteLine($"GetDetailResponse Unexpected error: {e.Message}\n{e.StackTrace}");
                return null;
            }
        }
        
    }
}