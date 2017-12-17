using NetPlugAndPlay.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public class RegisteredDevices
    {
        private static List<RegisteredDevice> Devices = new List<RegisteredDevice>();

        public static void Match(IPAddress deviceAddress, NetworkDevice networkDevice)
        {
            lock (Devices)
            {
                // TODO : make the time span an option
                var now = DateTimeOffset.Now;
                Devices.RemoveAll(x => x.TimeExpires <= now);
                Devices.RemoveAll(x => x.HostAddress == deviceAddress);
                Devices.Add(new RegisteredDevice
                {
                    HostAddress = deviceAddress,
                    NetworkDeviceId = networkDevice.Id,
                    TimeExpires = now.Add(TimeSpan.FromSeconds(300))
                });
            }
        }

        public static Guid FindRegisteredDevice(EndPoint endPoint)
        {
            if (endPoint is IPEndPoint)
                return FindRegisteredDevice((endPoint as IPEndPoint).Address);

            return Guid.Empty;
        }

        public static bool UnregisterDevice(IPAddress deviceAddress)
        {
            lock(Devices)
            {
                int count = Devices
                    .RemoveAll(x =>
                        x.HostAddress.Equals(deviceAddress)
                    );

                return count == 1;
            }
        }

        public static Guid FindRegisteredDevice(IPAddress deviceAddress)
        {
            lock (Devices)
            {
                var now = DateTimeOffset.Now;
                Devices.RemoveAll(x => x.TimeExpires <= now);

                Log.Logger.Here().Debug("Finding registered device " + deviceAddress.ToString());
                var match = Devices
                    .Where(x =>
                        x.HostAddress.Equals(deviceAddress)
                    )
                    .FirstOrDefault();

                if (match == null)
                {
                    Log.Logger.Here().Debug("Didn't find registered device " + deviceAddress.ToString());
                    return Guid.Empty;
                }
                Log.Logger.Here().Debug("Found registered device " + deviceAddress.ToString() + " as " + match.NetworkDeviceId.ToString());

                match.TimeExpires = now.Add(TimeSpan.FromSeconds(300));

                return match.NetworkDeviceId;
            }
        }
    }
}
