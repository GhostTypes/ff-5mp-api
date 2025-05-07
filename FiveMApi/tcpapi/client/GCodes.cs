namespace FiveMApi.tcpapi.client
{
    public class GCodes
    {
        public static readonly string CmdLogin = "~M601 S1";
        public static readonly string CmdLogout = "~M602";

        public static readonly string CmdPrintStatus = "~M27";
        public static readonly string CmdEndstopInfo = "~M119";
        public static readonly string CmdInfoStatus = "~M115";
        public static readonly string CmdInfoXyzab = "~M114";
        public static readonly string CmdTemp = "~M105";

        public static readonly string CmdLedOn = "~M146 r255 g255 b255 F0";
        public static readonly string CmdLedOff = "~M146 r0 g0 b0 F0";

        //public static readonly string CmdPrintStart = "~M23 0:/user/%%filename%%\r";
        //public static readonly string CmdPrintStop = "~M26";

        //public static readonly string CmdStartTransfer = "~M28 %%size%% 0:/user/%%filename%%\r";
        //public static readonly string CmdSaveFile = "~M29\r";

        public static readonly string CmdRunoutSensorOn = "~M405";
        public static readonly string CmdRunoutSensorOff = "~M406";

        public static readonly string CmdListLocalFiles = "~M661";
        public static readonly string CmdGetThumbnail = "~M662";

        public static readonly string TakePicture = "~M240";

        public static readonly string CmdHomeAxes = "~G28";
    }
}