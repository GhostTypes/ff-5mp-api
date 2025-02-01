using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FiveMApi.api.server;
using Newtonsoft.Json;

namespace FiveMApi.api.control.controls
{
    public class Files
    {
        private readonly FiveMClient _client;
        //private readonly Control _control;
        
        internal Files(FiveMClient printerClient)
        {
            _client = printerClient;
            //_control = printerClient.Control;
        }
        
        public class GCodeListResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("gcodeList")]
            public List<string> GCodeList { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }

        public async Task<List<string>> GetLocalFileList()
        {
            return await _client.TcpClient.GetFileListAsync();
        }
        
        /// <summary>
        /// Get a list of the 10 most recently printed files (quick)
        /// </summary>
        /// <returns></returns>
        public async Task<List<string>> GetRecentFileList()
        {
            var payload = new
            {
                serialNumber = _client.SerialNumber,
                checkCode = _client.CheckCode
            };
            await _client.HttpClientSemaphore.WaitAsync();
            var jsonPayload = JsonConvert.SerializeObject(payload);
            try
            {
                var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.GCodeList),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<GCodeListResponse>(data);
                if (result.Code == 0 && result.Message.Equals("Success", StringComparison.OrdinalIgnoreCase))
                    return result.GCodeList;

                Console.WriteLine($"Error retrieving file list: {result.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetFileList error: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                _client.ReleaseHttpClient();
            }
        }
        
        public class ThumbnailResponse
        {
            [JsonProperty("code")]
            public int Code { get; set; }

            [JsonProperty("imageData")]
            public string ImageData { get; set; }

            [JsonProperty("message")]
            public string Message { get; set; }
        }
        
        private readonly SemaphoreSlim _thumbnailSemaphore = new SemaphoreSlim(1, 8);

        private async Task ThumbnailWait()
        {
            await _thumbnailSemaphore.WaitAsync();
        }

        private void ThumbnailRelease()
        {
            _thumbnailSemaphore.Release();
        }
        
        /// <summary>
        /// Get the thumbnail for a file by name
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public async Task<byte[]> GetGCodeThumbnail(string fileName)
        {
            var payload = new
            {
                serialNumber = _client.SerialNumber,
                checkCode = _client.CheckCode,
                fileName
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);
            //await _client.HttpClientSemaphore.WaitAsync();
            await ThumbnailWait();
            try
            {
                var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.GCodeThumb),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<ThumbnailResponse>(data);
                if (result.Code == 0) return Convert.FromBase64String(result.ImageData);

                Console.WriteLine($"Error retrieving thumbnail: {result.Message}");
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"GetGcodeThumbnail error: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                ThumbnailRelease();
            }
        }
    }
}