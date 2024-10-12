using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FiveMApi.api
{
    public class FlashForgePrinter
    {
        public string Name { get; set; }
        public string SerialNumber { get; set; }
        public IPAddress IPAddress { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Serial: {SerialNumber}, IP: {IPAddress}";
        }
    }

    public class FlashForgePrinterDiscovery
    {
        private const int DiscoveryPort = 19000;

        public async Task<List<FlashForgePrinter>> DiscoverPrintersAsync(int timeoutMs = 10000, int idleTimeoutMs = 1500)
        {
            var printers = new List<FlashForgePrinter>();
            var broadcastAddresses = GetBroadcastAddresses().ToList();

            using (var udpClient = new UdpClient())
            {
                udpClient.EnableBroadcast = true;
                var discoveryMessage = Encoding.ASCII.GetBytes("discover");

                // Send discovery message to all broadcast addresses
                foreach (var broadcastAddress in broadcastAddresses)
                {
                    try
                    {
                        Debug.WriteLine("Broadcasting printer discovery to: " + broadcastAddress.Address);
                        await udpClient.SendAsync(discoveryMessage, discoveryMessage.Length, broadcastAddress);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to send to {broadcastAddress}: {ex.Message}");
                    }
                }

                try
                {
                    await ReceivePrinterResponses(udpClient, printers, timeoutMs, idleTimeoutMs);
                }
                catch (OperationCanceledException)
                {
                    // timeout
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"An error occurred: {ex.Message}");
                }
            }

            return printers;
        }

        private static async Task ReceivePrinterResponses(UdpClient udpClient, ICollection<FlashForgePrinter> printers, int totalTimeoutMs, int idleTimeoutMs)
        {
            using (var cts = new CancellationTokenSource(totalTimeoutMs))
            {
                var cancellationToken = cts.Token;
                try
                {
                    while (true)
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        try
                        {
                            var receiveTask = udpClient.ReceiveAsync();
                            var idleDelayTask = Task.Delay(idleTimeoutMs, cancellationToken);
                            var completedTask = await Task.WhenAny(receiveTask, idleDelayTask);

                            if (completedTask == receiveTask)
                            {
                                var result = receiveTask.Result;
                                var printer = ParsePrinterResponse(result.Buffer, result.RemoteEndPoint.Address);
                                if (printer != null)
                                {
                                    printers.Add(printer);
                                }
                                // Continue listening for more responses , idle timeout will reset on next iteration
                            }
                            else
                            {
                                break; // idle timeout
                            }
                        }
                        catch (OperationCanceledException)
                        {
                            break; // total timeout
                        }
                        catch (SocketException)
                        {
                            // todo impl
                            break;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"An error occurred while receiving responses: {ex.Message}");
                            break;
                        }
                    }
                }
                finally
                {
                    cts.Dispose();
                }
            }
        }

        private static FlashForgePrinter ParsePrinterResponse(byte[] response, IPAddress ipAddress)
        {
            Debug.WriteLine("Printer discovery response from: " + ipAddress);
            if (response == null || response.Length < 0xC4) return null;

            var name = Encoding.ASCII.GetString(response, 0, 32).TrimEnd('\0'); // Printer name (offset 0x00)
            var serialNumber = Encoding.ASCII.GetString(response, 0x92, 32).TrimEnd('\0'); // Serial number (offset 0x92)
            
            Debug.WriteLine("Valid printer: " + name + "(" + serialNumber + ")");
            
            return new FlashForgePrinter
            {
                Name = name,
                SerialNumber = serialNumber,
                IPAddress = ipAddress
            };
        }

        private static IEnumerable<IPEndPoint> GetBroadcastAddresses()
        { // get all broadcast addresses for discovery
            return (from ni in NetworkInterface.GetAllNetworkInterfaces() where ni.OperationalStatus == OperationalStatus.Up where ni.NetworkInterfaceType != NetworkInterfaceType.Loopback && ni.NetworkInterfaceType != NetworkInterfaceType.Tunnel from ua in ni.GetIPProperties().UnicastAddresses where ua.Address.AddressFamily == AddressFamily.InterNetwork let ipAddress = ua.Address let subnetMask = ua.IPv4Mask where subnetMask != null select GetBroadcastAddress(ipAddress, subnetMask) into broadcastAddress select new IPEndPoint(broadcastAddress, DiscoveryPort)).ToList();
        }

        private static IPAddress GetBroadcastAddress(IPAddress address, IPAddress subnetMask)
        {
            var ipAddressBytes = address.GetAddressBytes();
            var subnetMaskBytes = subnetMask.GetAddressBytes();

            if (ipAddressBytes.Length != subnetMaskBytes.Length)
                throw new ArgumentException("Address and subnet mask lengths do not match.");

            var broadcastAddress = new byte[ipAddressBytes.Length];
            for (var i = 0; i < broadcastAddress.Length; i++)
            {
                broadcastAddress[i] = (byte)(ipAddressBytes[i] | ~subnetMaskBytes[i]);
            }

            return new IPAddress(broadcastAddress);
        }

        private void PrintDebugInfo(byte[] response, IPAddress ipAddress)
        {
            Console.WriteLine($"Received response from {ipAddress}:");
            Console.WriteLine($"Response length: {response.Length} bytes");

            Console.WriteLine("Hex dump:"); // Hex dump
            for (var i = 0; i < response.Length; i += 16)
            {
                Console.Write($"{i:X4}   ");
                for (var j = 0; j < 16; j++)
                {
                    Console.Write(i + j < response.Length ? $"{response[i + j]:X2} " : "   ");
                    if (j == 7) Console.Write(" ");
                }
                Console.Write("  ");
                for (var j = 0; j < 16 && i + j < response.Length; j++)
                {
                    var c = (char)response[i + j];
                    Console.Write(char.IsControl(c) ? '.' : c);
                }
                Console.WriteLine();
            }

            Console.WriteLine("ASCII dump:"); // ASCII dump
            Console.WriteLine(Encoding.ASCII.GetString(response));
        }
    }
}
