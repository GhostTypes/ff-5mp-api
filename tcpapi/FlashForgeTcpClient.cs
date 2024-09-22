using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FiveMApi.tcpapi
{
    public class FlashForgeTcpClient : IDisposable
    {
        private Socket socket;
        private int port = 8899;
        private int timeout = 25000;
        protected string hostname;

        private NetworkStream _networkStream;

        public FlashForgeTcpClient(string hostname)
        {
            this.hostname = hostname;
            try
            {
                Debug.WriteLine("TcpPrinterClient creation");
                Connect();
                Debug.WriteLine("Connected");
            }
            catch (Exception)
            {
                Debug.WriteLine("TcpPrinterClient failed to init!!!");
            }
        }

        public async Task<string> SendCommandAsync(string cmd)
        {
            Debug.WriteLine("sendCommand: " + cmd);
            try
            {
                CheckSocket();
                CheckStream();
                var writer = new StreamWriter(_networkStream, Encoding.ASCII);
                await writer.WriteLineAsync(cmd);
                await writer.FlushAsync();
                var reply = await ReceiveMultiLineReplayAsync(cmd);
                if (reply != null) return reply;
                Debug.WriteLine("Invalid replay received, resetting connection to printer.");
                ResetSocket();
                CheckSocket();
                return null;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NetworkUnreachable)
            {
                var err = "Error while connecting. No route to host [" + ((IPEndPoint)socket.RemoteEndPoint).Address +
                          "].";
                Debug.WriteLine(err + "\n" + ex.StackTrace);
                return null;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
            {
                var err = "Error while connecting. Unknown host [" + ((IPEndPoint)socket.RemoteEndPoint).Address + "].";
                Debug.WriteLine(err + "\n" + ex.StackTrace);
                return null;
            }
            catch (IOException e)
            {
                var err = "Error while building or writing output stream:\n" + e.StackTrace;
                Debug.WriteLine(err);
                return null;
            }
        }

        private const string LineNPattern = @"N\d{4,}\sok";
        private static readonly Regex LineNRegex = new Regex(LineNPattern);

        public async Task<bool> SendRawDataAsync(List<byte[]> rawData)
        {
            try
            {
                CheckSocket();
                CheckStream();
                foreach (var bs in rawData)
                {
                    await _networkStream.WriteAsync(bs, 0, bs.Length);
                    var reply = await ReceiveSingleLineReplayAsync();
                    if (reply == null || !LineNRegex.Match(reply).Success) return false;
                }
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.NetworkUnreachable)
            {
                Debug.WriteLine("SendRawData failed, No route to host [" + ((IPEndPoint)socket.RemoteEndPoint).Address + "].");
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
            {
                Debug.WriteLine("SendRawData failed, Unknown host [" + ((IPEndPoint)socket.RemoteEndPoint).Address + "].");
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            catch (IOException e)
            {
                Debug.WriteLine("SendRawData failed, error while building/writing data to output stream");
                Debug.WriteLine(e.StackTrace);
                return false;
            }
            return true;
        }

        private void CheckSocket()
        {
            Debug.WriteLine("CheckSocket()");
            var fix = false;
            if (socket == null)
            {
                fix = true;
                Debug.WriteLine("TcpPrinterClient socket is null");
            }
            else if (!socket.Connected)
            {
                fix = true;
                Debug.WriteLine("TcpPrinterClient socket is closed");
            }

            if (!fix) return;

            Debug.WriteLine("Reconnecting to socket...");
            Connect();
        }

        private void CheckStream()
        {
            if (_networkStream == null) _networkStream = new NetworkStream(socket);
        }

        private void Connect()
        {
            Debug.WriteLine("Connect()");
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Connect(hostname, port);
            socket.ReceiveTimeout = timeout;
            _networkStream = new NetworkStream(socket);
        }

        private void ResetSocket()
        {
            Debug.WriteLine("ResetSocket()");
            _networkStream.Close();
            _networkStream = null;
            socket.Close();
            socket = null;
        }

        private async Task<string> ReceiveMultiLineReplayAsync(string cmd)
        {
            Console.WriteLine("ReceiveMultiLineReplayAsync()");
            var answer = new StringBuilder();
            try
            {
                if (_networkStream == null) _networkStream = new NetworkStream(socket);
                var reader = new StreamReader(_networkStream, Encoding.ASCII);

                string line;
                var doBreak = false;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Console.WriteLine(line);
                    answer.AppendLine(line);
                    // weird fix, but M661 sends its data back (file list) after ok
                    //if (cmd.Contains("M661") && line.Contains(".3mf")) break;
                    if (line.Equals("ok", StringComparison.OrdinalIgnoreCase) && !cmd.Contains("M661")) break;
                    if (cmd.Contains("M661") && line.Contains("~M662")) doBreak = true;
                    else if (doBreak) break;
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine("Error receiving multi-line command replay");
                Debug.WriteLine(e.StackTrace);
                return null;
            }

            var result = answer.ToString();
            Debug.WriteLine("Multi-line replay received:\n" + result);
            return result;
        }

        public async Task<List<string>> GetFileListAsync()
        {
            //const string cmd = "M661";
            var response = await SendCommandAsync("~M661");

            if (string.IsNullOrEmpty(response))
            {
                Debug.WriteLine("No response received for M661 command.");
                return null;
            }

            return ParseFileListResponse(response);
        }

        private List<string> ParseFileListResponse(string response)
        {
            var fileList = new List<string>();

            // The response contains control characters and symbols; we need to extract file paths.
            // Split the response using the delimiter "::??"
            var entries = response.Split(new[] { "::??" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                // Trim any leading/trailing whitespace and control characters
                var trimmedEntry = entry.Trim();

                // Find the index of "/data/"
                var dataIndex = trimmedEntry.IndexOf("/data/", StringComparison.OrdinalIgnoreCase);
                if (dataIndex >= 0)
                {
                    // Extract the file path starting from "/data/"
                    var filePath = trimmedEntry.Substring(dataIndex);

                    // Remove any non-printable characters from the file path
                    filePath = Regex.Replace(filePath, @"[^\u0020-\u007E]", string.Empty);

                    // Add to the list if it's not empty
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        fileList.Add(filePath);
                    }
                }
            }

            return fileList;
        }

        
        private async Task<string> ReceiveSingleLineReplayAsync()
        {
            try
            {
                if (_networkStream == null) _networkStream = new NetworkStream(socket);
                var reader = new StreamReader(_networkStream, Encoding.ASCII);
                var result = await reader.ReadLineAsync();
                if (result != null)
                {
                    result = result.Trim();
                    Debug.WriteLine("Single-line replay received:\n" + result);
                    return result;
                }
                Debug.WriteLine("Empty/null single-line replay received :(");
                return null;
            }
            catch (IOException e)
            {
                throw new Exception("Error while building or reading input stream.", e);
            }
        }

        public void Dispose()
        {
            try
            {
                Debug.WriteLine("TcpPrinterClient closing socket");
                socket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}
