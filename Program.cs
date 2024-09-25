using System;
using System.Threading.Tasks;
using FiveMApi.api;

namespace FiveMApi
{
    internal class Program
    {
        public static async Task Main(string[] args)
        {

            //var discovery = new FlashForgePrinterDiscovery();
            //var printers = await discovery.DiscoverPrintersAsync();

            //foreach (var printer in printers)
            //{
            //    Console.WriteLine(printer);
            //}
            
            /***var client = new FiveMClient("192.168.0.203", "SNMOMC9900728", "e5c2bf77");

            //await client.UploadFile("C:\\Users\\Cope\\Downloads\\Pot_PLA_1h39m.gcode", false, false);

            var info = await client.GetMachineInfo();
            Console.WriteLine($"Connected to {info.Name}");
            Console.WriteLine($"Firmware version: {info.FirmwareVersion}");
            Console.WriteLine($"Filament type: {info.FilamentType}");
            Console.WriteLine($"Nozzle size: {info.NozzleSize}");
            Console.WriteLine($"Status: {info.Status}");
            
            var printPercent = (int)Math.Round(info.PrintProgress * 100);
            
            //Console.Write($"estimatedTime: {info.EstimatedTime}");
            var eta = TimeSpan.FromSeconds(info.EstimatedTime);
            var etaStr = $"{(int)eta.TotalHours}h{eta.Minutes}m";
            
            Console.WriteLine($"Eta: {etaStr} ({printPercent}% done)");
            Console.WriteLine($"{(int)info.EstWeight}g used ({(int)info.EstLength} meters)");

            var bedTempStr = $"Print Bed: {(int)info.PrintBedTemp}/{info.PrintBedSetTemp}";
            var extTempStr = $"Extruder: {(int)info.ExtruderTemp}/{info.ExtruderSetTemp}";
            
            var doorOpen = info.DoorOpen ? "Yes" : "No";
            var lightsOn = info.LightsOn ? "Yes" : "No";
            
            Console.WriteLine(bedTempStr);
            Console.WriteLine(extTempStr);
            Console.WriteLine($"Door open: {doorOpen}");
            Console.WriteLine($"Lights on: {lightsOn}");***/




        }
    }
}