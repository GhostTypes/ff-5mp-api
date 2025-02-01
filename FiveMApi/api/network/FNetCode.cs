namespace FiveMApi.api.network
{
    public enum FNetCode
    {
        Ok = 0,
        Error = -1,
        AbortedByCallback = 1,
        DeviceBusy = 2,
        LanVerifyFail = 1001,
        WanVerifyFail = 2002,
        DeviceBound = 2003
    }
    
    /*
    FlashNetwork.h L303-310
    #define FNET_OK 0
    #define FNET_ERROR -1
    #define FNET_ABORTED_BY_CALLBACK 1
    #define FNET_DIVICE_IS_BUSY 2
    #define FNET_VERIFY_LAN_DEV_FAILED 1001 // invalid serialNumber/checkCode
    #define FNET_UNAUTHORIZED 2001          // invalid accessToken/clientAccessToken
    #define FNET_INVALID_VALIDATION 2002    // invalid userName/password/SMSCode
    #define FNET_DEVICE_HAS_BEEN_BOUND 2003
     */
}