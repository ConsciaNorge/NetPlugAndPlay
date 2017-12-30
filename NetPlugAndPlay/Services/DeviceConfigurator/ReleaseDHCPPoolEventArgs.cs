using libnetworkutility;
using System;


namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public class ReleaseDHCPPoolEventArgs : EventArgs
    {
        public NetworkPrefix Network { get; set; }
    }
}
