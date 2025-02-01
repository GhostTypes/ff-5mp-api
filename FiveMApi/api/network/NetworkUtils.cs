using System;
using FiveMApi.api.controls;

namespace FiveMApi.api.network
{
    public class NetworkUtils
    {

        public static bool IsOk(Control.GenericResponse response)
        {
            return (FNetCode)response.Code == FNetCode.Ok &&
                   response.Message.Equals("Success", StringComparison.OrdinalIgnoreCase);
        }
        
    }
}