using System;
using System.Diagnostics;

namespace FiveMApi.tcpapi.replays
{
    public class PrinterInfo
    {
        public string TypeName { get; set; }
        public string Name { get; set; }
        public string FirmwareVersion { get; set; }
        public string SerialNumber { get; set; }
        public string Dimensions { get; set; }
        public string MacAddress { get; set; }
        
        public string ToolCount { get; set; } // unused but in FlashForge fw

        public bool IsPro()
        {
            return TypeName.Contains("Pro");
        }

        /**
         * Create a PrinterInfo instance from an M115 command replay
         */
        public PrinterInfo FromReplay(string replay)
        {
            if (replay == null) return null;
            try
            {
                var data = replay.Split('\n');
                var name = GetRight(data[1]);
                if (name == null)
                {
                    Debug.WriteLine("PrinterInfo replay has null Machine Type");
                    return null;
                }
                TypeName = name;
                
                var nick = GetRight(data[2]);
                if (nick == null)
                {
                    Debug.WriteLine("PrinterInfo replay has null Machine Name");
                    return null;
                }
                Name = nick;
                
                var fw = GetRight(data[3]);
                if (fw == null)
                {
                    Debug.WriteLine("PrinterInfo replay has null firmware version");
                    return null;
                }
                FirmwareVersion = fw;
                
                var sn = GetRight(data[4]);
                if (sn == null)
                {
                    Debug.WriteLine("PrinterInfo replay has null serial number");
                    return null;
                }
                SerialNumber = sn;
                
                Dimensions = data[5].Trim();
                var tcs = GetRight(data[6]);
                if (tcs == null)
                {
                    Debug.WriteLine("PrinterInfo replay has null tool count");
                    return null;
                }

                ToolCount = tcs;
                MacAddress = data[7].Replace("Mac Address:", "");
                return this;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error creating PrinterInfo instance from replay");
                Debug.WriteLine(e.StackTrace);
                return null;
            }
        }

        private static string GetRight(string rpData)
        {
            try
            {
                return rpData.Split(':')[1].Trim();
            }
            catch (Exception)
            {
                return null;
            }
        }

        public override string ToString()
        {
            return "Printer Type: " + TypeName + "\n" +
                   "Name: " + Name + "\n" +
                   "Firmware: " + FirmwareVersion + "\n" +
                   "Serial Number: " + SerialNumber + "\n" +
                   "Print Dimensions: " + Dimensions + "\n" +
                   "Tool Count: " + ToolCount + "\n" +
                   "MAC Address: " + MacAddress;
        }
    }
}