using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using FiveMApi.api.server;
using Newtonsoft.Json;

namespace FiveMApi.api.controls
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

        /**public async Task<bool> IsReady()
        {
            var info = await Get();
            return info.Status == "ready";
        }**/
        
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
                var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.Detail),
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
        
    }
}