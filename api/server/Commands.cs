namespace FiveMApi.api.server
{
    public class Commands
    {
        public const string LightControlCmd = "lightControl_cmd";
        public const string PrinterControlCmd = "printerCtl_cmd";
        public const string CirculationControlCmd = "circulateCtl_cmd";
        public const string JobControlCmd = "jobCtl_cmd";
        
        // todo testing
        public const string CameraControlCmd = "streamCtrl_cmd";
        
        public const string DeviceUnregisterCmd = "deviceUnregister_cmd";
        public const string UserUnregisterCmd = "userUnregister_cmd";
        public const string DeviceCmd = "device_cmd";
        public const string ClearFanCmd = "clearFan_cmd";
        
        
        public const string TempControlCmd = "temperatureCtl_cmd";
        /* typedef struct fnet_temp_ctrl {
            double platformTemp;
            double rightTemp;
            double leftTemp;
            double chamberTemp;
        } fnet_temp_ctrl_t; */
    }
}