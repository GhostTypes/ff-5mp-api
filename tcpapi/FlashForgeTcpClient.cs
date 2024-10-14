using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FiveMApi.tcpapi
{
    public class FlashForgeTcpClient : IDisposable
    {
        private Socket socket;
        private int port = 8899;
        private int timeout = 5000;
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

        private CancellationTokenSource _keepAliveCancellationTokenSource;

        private int keepAliveErrors;

        public void StartKeepAlive()
        {
            if (_keepAliveCancellationTokenSource != null)
            {
                // Keep-alive task is already running
                return;
            }

            _keepAliveCancellationTokenSource = new CancellationTokenSource();
            var token = _keepAliveCancellationTokenSource.Token;

            Task.Run(async () =>
            {
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        Debug.WriteLine("KeepAlive");
                        if (await SendCommandAsync("~M27") == null)
                        {
                            // keep alive failed, connection error/timeout etc
                            keepAliveErrors++; // keep track of errors
                            Debug.WriteLine($"Current keep alive failure: {keepAliveErrors}");
                            break;
                        }

                        if (keepAliveErrors > 0) keepAliveErrors--; // move back to 0 errors with each "good" keep-alive
                        // increase keep alive timeout based on error count
                        // in normal situations even with a meh wifi speed/router, 5000ms is (should be) plenty of time
                        // other API/UI etc traffic may also affect this/vice versa. still working out the kinks
                        // the printer also seems to get "overloaded" sometimes when actively printing, but haven't narrowed it down
                        // to anything constant
                        await Task.Delay(5000 + keepAliveErrors * 1000, token);
                    }
                }
                catch (TaskCanceledException)
                {
                    // Task was canceled, no action needed
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("KeepAlive encountered an exception: " + ex.Message);
                }
            }, token);
        }

        public void StopKeepAlive(bool logout = false)
        {
            if (_keepAliveCancellationTokenSource == null) return;
            _keepAliveCancellationTokenSource.Cancel();
            _keepAliveCancellationTokenSource.Dispose();
            _keepAliveCancellationTokenSource = null;
            Debug.WriteLine("Keep-alive stopped.");
            if (logout) Task.Run(async () => { await SendCommandAsync("~M602"); });
        }

        private readonly SemaphoreSlim _socketSemaphore = new SemaphoreSlim(1, 1);

        public async Task<string> SendCommandAsync(string cmd)
        {
            await _socketSemaphore.WaitAsync();
            Debug.WriteLine("sendCommand: " + cmd);
            try
            {
                CheckSocket();
                CheckStream();
                var writer = new StreamWriter(_networkStream, Encoding.ASCII);
                await writer.WriteLineAsync(cmd);
                await writer.FlushAsync();
                var reply = await ReceiveMultiLineReplayAsync(cmd);
                if (reply != null) return reply; // null checked in ReceiveMultiLineReplayAsync but doesn't hurt
                Debug.WriteLine("Invalid or no replay received, resetting connection to printer.");
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
            finally
            {
                _socketSemaphore.Release();
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
                Debug.WriteLine("SendRawData failed, No route to host [" + ((IPEndPoint)socket.RemoteEndPoint).Address +
                                "].");
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.HostNotFound)
            {
                Debug.WriteLine("SendRawData failed, Unknown host [" + ((IPEndPoint)socket.RemoteEndPoint).Address +
                                "].");
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
            StartKeepAlive(); // start this here rather than Connect(), because Connect() is called in the constructor
            // this will only be called after creation (socket error, reset, etc.)
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
            StopKeepAlive();
            _networkStream.Close();
            _networkStream = null;
            socket.Close();
            socket = null;
        }

        private async Task<string> ReceiveMultiLineReplayAsync(string cmd)
        {
            Debug.WriteLine("ReceiveMultiLineReplayAsync()");
            var answer = new List<byte>();
            try
            {
                if (_networkStream == null)
                    _networkStream = new NetworkStream(socket);

                var buffer = new byte[4096];
                var timeoutMs = 5000; // Adjusted timeout to 5 seconds
                var timeoutCts = new CancellationTokenSource(timeoutMs);

                while (true)
                {
                    int bytesRead;
                    try
                    {
                        // Read data with the specified timeout
                        var readTask = _networkStream.ReadAsync(buffer, 0, buffer.Length, timeoutCts.Token);
                        bytesRead = await readTask;
                    }
                    catch (OperationCanceledException)
                    {
                        Debug.WriteLine("ReceiveMultiLineReplayAsync timed out.");
                        break;
                    }

                    if (bytesRead == 0)
                    {
                        // No more data available
                        break;
                    }

                    answer.AddRange(buffer.Take(bytesRead));

                    // Convert the data so far to string using UTF8 encoding
                    var dataSoFar = Encoding.ASCII.GetString(answer.ToArray());

                    // Check for end of response
                    if ((cmd.Equals("~M661") && dataSoFar.Contains("~M662")) ||
                        (!cmd.Equals("~M661") && dataSoFar.Contains("ok")))
                    {
                        break;
                    }

                    // If no more data is available and the end marker hasn't been found, exit the loop
                    if (socket.Available == 0)
                    {
                        break;
                    }
                }
            }
            catch (IOException e)
            {
                Debug.WriteLine("Error receiving multi-line command reply");
                Debug.WriteLine(e.StackTrace);
                return null;
            }

            var result = Encoding.UTF8.GetString(answer.ToArray());
            if (string.IsNullOrEmpty(result))
            {
                Debug.WriteLine("ReceiveMultiLineReplayAsync received an empty response.");
                return null;
            }

            Debug.WriteLine("Multi-line replay received:\n" + result);
            return result;
        }


        public async Task<List<string>> GetFileListAsync()
        {
            var response = await SendCommandAsync("~M661");
            //ResetSocket();
            //await Task.Delay(500);
            //CheckSocket(); // reconnect socket + re-start keep-alive
            //await Task.Delay(250);
            //await SendCommandAsync("~M601 S1");
            //await Task.Delay(250);

            if (!string.IsNullOrEmpty(response)) return ParseFileListResponse(response);
            Debug.WriteLine("No response received for M661 command.");
            return null;
        }

        private List<string> ParseFileListResponse(string response)
        {
            // Use regular expressions to match file entries if the data format is consistent
            var fileList = new List<string>();

            // Split the response by the file separator
            var entries = response.Split(new[] { "::" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                var trimmedEntry = entry.Trim();
                var dataIndex = trimmedEntry.IndexOf("/data/", StringComparison.OrdinalIgnoreCase);
                if (dataIndex >= 0)
                {
                    var filePath = trimmedEntry.Substring(dataIndex);
                    // Remove non-printable characters
                    filePath = Regex.Replace(filePath, @"[^\u0020-\u007E]", string.Empty);
                    if (!string.IsNullOrEmpty(filePath))
                    {
                        fileList.Add(filePath.Replace("/data/", ""));
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
                _networkStream?.Dispose();
                socket?.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}