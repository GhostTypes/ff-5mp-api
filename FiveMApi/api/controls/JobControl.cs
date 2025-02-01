using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FiveMApi.api.server;
using Newtonsoft.Json;

namespace FiveMApi.api.controls
{
    public class JobControl
    {
        private readonly FiveMClient _client;
        private readonly Control _control;
        
        internal JobControl(FiveMClient printerClient)
        {
            _client = printerClient;
            _control = printerClient.Control;
        }
        
        // basic controls
        public async Task<bool> PausePrintJob()
        {
            return await _control.SendJobControlCmd("pause");
        }

        public async Task<bool> ResumePrintJob()
        {
            return await _control.SendJobControlCmd("continue");
        }

        public async Task<bool> CancelPrintJob()
        {
            return await _control.SendJobControlCmd("cancel");
        }
        
        /// <summary>
        /// Upload a GCode/3MF file to the printer
        /// </summary>
        /// <param name="filePath">Path to the file to upload</param>
        /// <param name="startPrint">Start the printer after uploading</param>
        /// <param name="levelBeforePrint">Level the bed before printing</param>
        public async Task<bool> UploadFile(string filePath, bool startPrint, bool levelBeforePrint)
        {
            var fileInfo = new FileInfo(filePath);
            var fileSize = fileInfo.Length;

            using (var content = new MultipartFormDataContent("------------------------DHD3lr8XwXBuyC8G3dWjK7"))
            {
                content.Headers.Add("serialNumber", _client.SerialNumber);
                content.Headers.Add("checkCode", _client.CheckCode);
                content.Headers.Add("fileSize", fileSize.ToString());
                content.Headers.Add("printNow", startPrint.ToString().ToLower());
                content.Headers.Add("levelingBeforePrint", levelBeforePrint.ToString().ToLower());

                var fileContent = new StreamContent(fileInfo.OpenRead());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"gcodeFile\"",
                    FileName = "\"" + fileInfo.Name + "\""
                };
                content.Add(fileContent);

                _client.HttpClient.DefaultRequestHeaders.ExpectContinue = true;
                try
                {
                    var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.UploadFile), content);
                    response.EnsureSuccessStatusCode();
                    var data = await response.Content.ReadAsStringAsync();
                    var result = JsonConvert.DeserializeObject<Control.GenericResponse>(data);
                    _client.HttpClient.DefaultRequestHeaders.ExpectContinue = false;
                    return result.Message.Equals("Success");
                }
                catch (Exception e)
                {
                    _client.HttpClient.DefaultRequestHeaders.ExpectContinue = false;
                    Console.WriteLine($"UploadFile error: {e.Message}\n{e.StackTrace}");
                    return false;
                }
            }
        }
        
        public async Task<bool> PrintLocalFile(string fileName, bool levelingBeforePrint)
        {
            var payload = new
            {
                serialNumber = _client.SerialNumber,
                checkCode = _client.CheckCode,
                fileName,
                levelingBeforePrint
            };

            var jsonPayload = JsonConvert.SerializeObject(payload);

            try
            {
                var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.GCodePrint),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode(); // Throws if not 2XX

                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Control.GenericResponse>(data);

                return result.Code == 0 && result.Message.Equals("Success", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception e)
            {
                Console.WriteLine($"PrintGcodeFile error: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }
    }
}