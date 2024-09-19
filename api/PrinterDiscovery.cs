using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        private const string BroadcastAddress = "192.168.0.255";

        public async Task<List<FlashForgePrinter>> DiscoverPrintersAsync(int timeoutMs = 5000)
        {
            var printers = new List<FlashForgePrinter>();
            using (var udpClient = new UdpClient())
            {
                udpClient.EnableBroadcast = true;

                // Send broadcast
                var discoveryMessage = Encoding.ASCII.GetBytes("discover");
                await udpClient.SendAsync(discoveryMessage, discoveryMessage.Length, BroadcastAddress, DiscoveryPort);

                // Listen for responses
                var receiveTask = ReceivePrinterResponses(udpClient, printers);
                if (await Task.WhenAny(receiveTask, Task.Delay(timeoutMs)) == receiveTask)
                {
                    await receiveTask;
                }
            }

            return printers;
        }

        private async Task ReceivePrinterResponses(UdpClient udpClient, List<FlashForgePrinter> printers)
        {
            while (true)
            {
                try
                {
                    var result = await udpClient.ReceiveAsync();
                    var printer = ParsePrinterResponse(result.Buffer, result.RemoteEndPoint.Address);
                    if (printer != null)
                    {
                        printers.Add(printer);
                    }
                }
                catch (SocketException)
                {
                    // Timeout or error occurred
                    break;
                }
            }
        }

        private FlashForgePrinter ParsePrinterResponse(byte[] response, IPAddress ipAddress)
        {
        
            //PrintDebugInfo(response, ipAddress);

            // Extract printer name (starts at offset 0x00)
            string name = Encoding.ASCII.GetString(response, 0, 32).TrimEnd('\0');

            // Extract serial number (starts at offset 0x92)
            string serialNumber = Encoding.ASCII.GetString(response, 0x92, 32).TrimEnd('\0');

            return new FlashForgePrinter
            {
                Name = name,
                SerialNumber = serialNumber,
                IPAddress = ipAddress
            };
        }

        private void PrintDebugInfo(byte[] response, IPAddress ipAddress)
        {
            Console.WriteLine($"Received response from {ipAddress}:");
            Console.WriteLine($"Response length: {response.Length} bytes");

            // Print hexadecimal representation
            Console.WriteLine("Hex dump:");
            for (int i = 0; i < response.Length; i += 16)
            {
                Console.Write($"{i:X4}   ");
                for (int j = 0; j < 16; j++)
                {
                    if (i + j < response.Length)
                        Console.Write($"{response[i + j]:X2} ");
                    else
                        Console.Write("   ");
                    if (j == 7) Console.Write(" ");
                }
                Console.Write("  ");
                for (int j = 0; j < 16 && i + j < response.Length; j++)
                {
                    char c = (char)response[i + j];
                    Console.Write(char.IsControl(c) ? '.' : c);
                }
                Console.WriteLine();
            }

            // Print ASCII representation
            Console.WriteLine("ASCII dump:");
            Console.WriteLine(Encoding.ASCII.GetString(response));
        }
    }
}