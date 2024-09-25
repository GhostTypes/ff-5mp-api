using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Drawing;
using FiveMApi.api.controls;
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

        public string PrinterName { get; private set; }
        public string FirmwareVersion { get; private set; }

        public string IpAddress { get;}
        public string MacAddress { get; private set; }
        
        public string FlashCloudCode { get; private set; }
        public string PolarCloudCode { get; private set; }
        
        public string LifetimePrintTime { get; private set; }
        public string LifetimeFilamentMeters { get; private set; }

        public FiveMClient(string ipAddress, string serialNumber, string checkCode)
        {
            IpAddress = ipAddress;
            SerialNumber = serialNumber;
            CheckCode = checkCode;

            HttpClient = new HttpClient();
            HttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("*/*"));

            TcpClient = new FlashForgeClient(ipAddress);
            Control = new Control(this);
            JobControl = new JobControl(this);
            Info = new Info(this);
            Files = new Files(this);
        }

        public async Task<bool> InitControl()
        {
            return await TcpClient.InitControl();
        }

        public async Task<bool> CacheDetails()
        {
            var info = await Info.Get();
            if (info == null) return false;
            PrinterName = info.Name;
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
            var details = await Info.GetDetailResponse();
            return details != null && details.Message.Equals("Success");
        }
        
    }
}
