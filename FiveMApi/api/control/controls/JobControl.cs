﻿using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using FiveMApi.api.controls;
using FiveMApi.api.network;
using FiveMApi.api.server;
using Newtonsoft.Json;

namespace FiveMApi.api.control.controls
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

        // Check for firmware 3.1.3+
        private bool IsNewFirmwareVersion()
        {
            try
            {
                var minVersion = new Version(3, 1, 3);
                return _client.FirmVer >= minVersion;
            }
            catch
            {
                // todo error handling
                return false;
            }
        }

        public async Task<bool> ClearPlatform()
        {
            var args = new
            {
                action = "setClearPlatform"
            };
            return await _control.SendControlCommand("stateCtrl_cmd", args);
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

            await _client.HttpClientSemaphore.WaitAsync();
            try
            {
                MultipartFormDataContent content;
                if (IsNewFirmwareVersion())
                {
                    // New format for firmware >= 3.1.3
                    content = new MultipartFormDataContent("------------------------v3GcLTGebpPzgLGBOgAQKJ");
                    content.Headers.Add("serialNumber", _client.SerialNumber);
                    content.Headers.Add("checkCode", _client.CheckCode);
                    content.Headers.Add("fileSize", fileSize.ToString());
                    content.Headers.Add("printNow", startPrint.ToString().ToLower());
                    content.Headers.Add("levelingBeforePrint", levelBeforePrint.ToString().ToLower());
                    content.Headers.Add("flowCalibration", "false"); // todo impl these, assuming all are for the 5X only.
                    content.Headers.Add("useMatlStation", "false");
                    content.Headers.Add("gcodeToolCnt", "0");
                    content.Headers.Add("materialMappings", "W10="); // Base64 encoded empty array "[]"
                }
                else
                {
                    // Old format for firmware < 3.1.3
                    content = new MultipartFormDataContent("------------------------DHD3lr8XwXBuyC8G3dWjK7");
                    content.Headers.Add("serialNumber", _client.SerialNumber);
                    content.Headers.Add("checkCode", _client.CheckCode);
                    content.Headers.Add("fileSize", fileSize.ToString());
                    content.Headers.Add("printNow", startPrint.ToString().ToLower());
                    content.Headers.Add("levelingBeforePrint", levelBeforePrint.ToString().ToLower());
                }

                var fileContent = new StreamContent(fileInfo.OpenRead());
                fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                fileContent.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                {
                    Name = "\"gcodeFile\"",
                    FileName = "\"" + fileInfo.Name + "\""
                };
                content.Add(fileContent);

                _client.HttpClient.DefaultRequestHeaders.ExpectContinue = true;

                var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.UploadFile), content);
                response.EnsureSuccessStatusCode();
                var data = await response.Content.ReadAsStringAsync();
                Debug.WriteLine("UploadFile raw response: " + data);
                var result = JsonConvert.DeserializeObject<Control.GenericResponse>(data);
                _client.HttpClient.DefaultRequestHeaders.ExpectContinue = false;
                return NetworkUtils.IsOk(result);
            }
            catch (Exception e)
            {
                _client.HttpClient.DefaultRequestHeaders.ExpectContinue = false;
                Debug.WriteLine($"UploadFile error: {e.Message}\n{e.StackTrace}");
                return false;
            }
            finally
            {
                _client.HttpClientSemaphore.Release();
            }
        }

        public async Task<bool> PrintLocalFile(string fileName, bool levelingBeforePrint)
        {
            object payload;
            if (IsNewFirmwareVersion())
            {
                // New format for firmware >= 3.1.3
                payload = new
                {
                    serialNumber = _client.SerialNumber,
                    checkCode = _client.CheckCode,
                    fileName,
                    levelingBeforePrint,
                    flowCalibration = false, // todo impl these, assuming all are for the 5X only.
                    useMatlStation = false,
                    gcodeToolCnt = 0,
                    materialMappings = new object[] { } // Empty array for materialMappings
                };
            }
            else
            {
                // Old format for firmware < 3.1.3
                payload = new
                {
                    serialNumber = _client.SerialNumber,
                    checkCode = _client.CheckCode,
                    fileName,
                    levelingBeforePrint
                };
            }

            var jsonPayload = JsonConvert.SerializeObject(payload);

            await _client.HttpClientSemaphore.WaitAsync();
            try
            {
                var response = await _client.HttpClient.PostAsync(_client.GetEndpoint(Endpoints.GCodePrint),
                    new StringContent(jsonPayload, Encoding.UTF8, "application/json"));
                response.EnsureSuccessStatusCode();

                var data = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<Control.GenericResponse>(data);
                return NetworkUtils.IsOk(result);
            }
            catch (Exception e)
            {
                Debug.WriteLine($"PrintLocalFile error: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                _client.HttpClientSemaphore.Release();
            }

            return false;
        }
    }
}