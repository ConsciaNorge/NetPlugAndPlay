using NetPlugAndPlay.Services.DeviceConfigurator.ViewModels;
using System;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public class ChangeDHCPPoolEventArgs : EventArgs
    {
        public DHCPPoolChangeViewModel Changes { get; set; }
    }
}
