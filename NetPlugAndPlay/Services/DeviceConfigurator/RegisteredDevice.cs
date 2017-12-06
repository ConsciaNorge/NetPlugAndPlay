using System;
using System.Net;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public class RegisteredDevice
    {
        public IPAddress HostAddress { get; set; }
        public Guid NetworkDeviceId { get; set; }
        public DateTimeOffset TimeExpires { get; set; }
    }
}
