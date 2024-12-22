using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Drawing;
using System.Threading;
using FiveMApi.api.control.controls;
using FiveMApi.api.controls;
using FiveMApi.api.server;
using FiveMApi.tcpapi;
using SkiaSharp;

namespace FiveMApi.api
{
    public partial class FiveMClient
    {
        private const int Port = 8898;


        public Control Control { get; private set; } // general controls
        public JobControl JobControl { get; private set; } // uploading job/starting local job, pause stop resume, etc.
        public Info Info { get; private set; } // parsing the full api response (for use elsewhere)

        public Files Files { get; private set; } // get recent files / local jobs, thumbnail data

        public TempControl TempControl { get; private set; } // setting extruder/bed temp, etc.

        public FlashForgeClient TcpClient { get; }

        public string SerialNumber { get; }

        public string CheckCode { get; }

        public HttpClient HttpClient { get; }

        public readonly SemaphoreSlim HttpClientSemaphore = new SemaphoreSlim(1, 1);

        public async Task<bool> IsHttpClientBusy()
        {
            var isBusy = !await HttpClientSemaphore.WaitAsync(0);
            if (isBusy) return true;
            HttpClientSemaphore.Release(); // If acquired, release the semaphore
            return false;
        }

        public void ReleaseHttpClient()
        {
            HttpClientSemaphore.Release();
        }

        public string PrinterName { get; private set; }
        public bool IsPro { get; private set; }
        public string FirmwareVersion { get; private set; }

        public string IpAddress { get; }
        public string MacAddress { get; private set; }

        public string FlashCloudCode { get; private set; }
        public string PolarCloudCode { get; private set; }

        public string LifetimePrintTime { get; private set; }
        public string LifetimeFilamentMeters { get; private set; }
        
        
        // control states (for non pro models and possibly other backward compatibility)
        public bool LedControl { get; private set; }
        public bool FiltrationControl { get; private set; }

        public FiveMClient(string ipAddress, string serialNumber, string checkCode)
        {
            IpAddress = ipAddress;
            SerialNumber = serialNumber;
            CheckCode = checkCode;

            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));
            HttpClient.Timeout = TimeSpan.FromSeconds(5);

            TcpClient = new FlashForgeClient(ipAddress);
            Control = new Control(this);
            JobControl = new JobControl(this);
            Info = new Info(this);
            Files = new Files(this);
            TempControl = new TempControl(this);
            // initial cache
            Task.Run(CacheDetails);
        }

        public async Task<bool> InitControl()
        {
            Debug.WriteLine("InitControl()");
            if (await SendProductCommand()) return await TcpClient.InitControl();
            Debug.WriteLine("New API control failed!");
            return false;

        }

        public void Dispose()
        {
            TcpClient.StopKeepAlive(true);
            TcpClient.Dispose();
        }

        public async Task<bool> CacheDetails()
        {
            var info = await Info.Get();
            if (info == null) return false;
            PrinterName = info.Name;
            IsPro = PrinterName.Contains("Pro"); // check for pro-model specifics
            Console.WriteLine("IsPro: " + IsPro);
            FirmwareVersion = info.FirmwareVersion;
            MacAddress = info.MacAddress;
            FlashCloudCode = info.FlashCloudRegisterCode;
            PolarCloudCode = info.PolarCloudRegisterCode;
            LifetimePrintTime = info.FormattedTotalRunTime;
            LifetimeFilamentMeters = $"{info.CumulativeFilament:F2}m";
            return true;
        }

        public string GetEndpoint(string endpoint)
        {
            return $"http://{IpAddress}:{Port}{endpoint}";
        }


        public async Task<bool> VerifyConnection()
        {
            await CacheDetails(); // cache these immediately (for use in UI)
            var details = await Info.GetDetailResponse();
            return details != null && details.Message.Equals("Success");
        }

        public async Task<bool> SendProductCommand()
        {
            Debug.WriteLine("SendProductCommand()");
            await HttpClientSemaphore.WaitAsync();
            var payload = new
            {
                serialNumber = SerialNumber,
                checkCode = CheckCode
            };
            try
            {
                var response = await HttpClient.PostAsync(GetEndpoint(Endpoints.Product),
                    new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json"));
                var data = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode) return false;
                try
                {
                    var productResponse = JsonConvert.DeserializeObject<ProductResponse>(data);
                    if (productResponse != null && productResponse.Code.Equals(0))
                    {
                        // parse & set control states
                        var product = productResponse.Product;
                        LedControl = product.LightCtrlState != 0;
                        if (product.InternalFanCtrlState == 0 || product.ExternalFanCtrlState == 0)
                            FiltrationControl = false;
                        else FiltrationControl = true;
                        Debug.WriteLine("LedControl: " + LedControl);
                        Debug.WriteLine("FiltrationControl: " + FiltrationControl);
                        return true;
                    }
                }
                catch (Exception e) { Debug.WriteLine($"SendProductCommand failure: {e.Message}\n{e.StackTrace}"); }
            }
            catch (Exception e) { Debug.WriteLine($"SendProductCommand failure: {e.Message}\n{e.StackTrace}"); }
            finally { HttpClientSemaphore.Release(); }
            return false;
        }
        
        private class ProductResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }

            [JsonProperty("product")]
            public Product Product { get; set; }
        }
        
        // need to do more testing/wireshark sniffing to verify what these indicate
        private class Product
        {
            [JsonProperty("chamberTempCtrlState")]
            public int ChamberTempCtrlState { get; set; }

            [JsonProperty("externalFanCtrlState")]
            public int ExternalFanCtrlState { get; set; }

            [JsonProperty("internalFanCtrlState")]
            public int InternalFanCtrlState { get; set; }

            [JsonProperty("lightCtrlState")]
            public int LightCtrlState { get; set; }

            [JsonProperty("nozzleTempCtrlState")]
            public int NozzleTempCtrlState { get; set; }

            [JsonProperty("platformTempCtrlState")]
            public int PlatformTempCtrlState { get; set; }
        }
    }
}