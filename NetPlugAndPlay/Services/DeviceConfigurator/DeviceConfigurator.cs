using libterminal.Helpers.Model;
using libterminal.Helpers.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetPlugAndPlay.Models;
using NetPlugAndPlay.Services.SyslogServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NetPlugAndPlay.Services.DHCP_Server;
using libterminal;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public partial class DeviceConfigurator
    {
        public static string DeviceConfiguredLogMessage = "Device configured";

        private string GetPhysicalInterfaceName(string interfaceName)
        {
            // TODO : Switch to using a parser for this

            var parts = interfaceName.Split('.');
            if (parts == null || parts.Count() == 1)
                return interfaceName;

            return parts[0];
        }

        private async Task<NetworkDevice> MatchCDPEntry(PnPServerContext dbContext, ShowCDPEntryItem cdpEntry)
        {
            var deviceId = cdpEntry.DeviceID.ToLowerInvariant();

            var neighborDevice = await dbContext.NetworkDevices
                .Where(x =>
                    x.Hostname.ToLower() == cdpEntry.DeviceID ||
                    (x.Hostname + "." + x.DomainName).ToLowerInvariant() == deviceId
                )
                .Include("DeviceType")
                .Include("DeviceType.Interfaces")
                .FirstOrDefaultAsync();

            if (neighborDevice == null)
                return null;

            var remotePortId = GetPhysicalInterfaceName(cdpEntry.PortId.ToLowerInvariant());

            var remoteInterface = neighborDevice.DeviceType.Interfaces
                .Where(x =>
                    x.Name.ToLowerInvariant() == remotePortId
                )
                .FirstOrDefault();

            if (remoteInterface == null)
                return null;

            var uplink = await dbContext.NetworkDeviceLinks
                .Where(x =>
                    x.ConnectedToDevice.Id == neighborDevice.Id &&
                    x.ConnectedToInterfaceIndex == remoteInterface.InterfaceIndex
                )
                .Include("NetworkDevice")
                .FirstOrDefaultAsync();

            if (uplink == null)
                return null;

            return uplink.NetworkDevice;
        }

        internal Task ForgetIP(object sender, IPReleasedEventArgs ev)
        {
            return Task.Run(() => {
                System.Diagnostics.Debug.WriteLine("Unregistering device with IP " + ev.Address.ToString() + " from device manager");
                if (RegisteredDevices.UnregisterDevice(ev.Address))
                    System.Diagnostics.Debug.WriteLine("Device with IP " + ev.Address.ToString() + " successfully unregistered from device manager");
                else
                    System.Diagnostics.Debug.WriteLine("Device with IP " + ev.Address.ToString() + " either was not registered with device manager or could not be unregistered");

                if (ConnectionManager.Instance.RemoveConnectionsByHost(ev.Address.ToString()))
                    System.Diagnostics.Debug.WriteLine("Closed telnet and ssh connections to " + ev.Address.ToString());
                else
                    System.Diagnostics.Debug.WriteLine("No telnet or ssh connections were closed to " + ev.Address.ToString());
            });
        }

        void TransferConfigurationToDevice(IPAddress address)
        {
            var uri = new Uri("telnet://initialConfig:Minions12345@" + address.ToString());
            var copyResult = libterminal.Helpers.TaskCopy.Run(
                uri,
                "Minions12345",
                "tftp://10.100.11.55/config.txt",
                "running-config"
            );
            System.Diagnostics.Debug.WriteLine("Copied config");
        }

        public async Task SyslogMessageHandler(object sender, SyslogMessageEventArgs args)
        {
            string connectionString;
            try
            {
                // TODO : Make sure that this code doesn't run until the startup is complete
                connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to get database connection string");
                return;
            }

            if (!args.Message.Contains(DeviceConfiguredLogMessage))
                return;

            System.Diagnostics.Debug.WriteLine("Device : " + args.Host.ToString() + " signaled it is ready to be identified");

            if (!await IdentifyDevice(args.Host))
            {
                System.Diagnostics.Debug.WriteLine("Failed to identify device : " + args.Host.ToString() + " as CDP neighbors either cannot be resolved or are unknown.");
                return;
            }

            return;
        }

        public async Task<bool> IdentifyDevice(EndPoint client)
        {
            string connectionString;
            try
            {
                // TODO : Make sure that this code doesn't run until the startup is complete
                connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Failed to get database connection string");
                return false;
            }

            var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
            dbOptions.UseSqlServer(connectionString);

            var dbContext = new PnPServerContext(dbOptions.Options);

            var hostName = string.Empty;
            if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                hostName = (client as IPEndPoint).Address.ToString();
            else
            {
                throw new Exception("Address family not supported");
            }
            var uri = new Uri("telnet://initialConfig:Minions12345@" + hostName);

            var entriesText = libterminal.Helpers.TaskShowCDPEntries.Run(
                uri,
                "Minions12345"
            );

            if (string.IsNullOrWhiteSpace(entriesText))
                System.Diagnostics.Debug.WriteLine("Received null or whitespace result to 'show cdp entries *'");
            else
            {
                System.Diagnostics.Debug.WriteLine("Received CDP entries");
                var parser = new ShowCDPEntry();
                List<ShowCDPEntryItem> entries = null;
                try
                {
                    entries = parser.Parse(entriesText);
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to parse CDP entries : " + e.Message);
                    return false;
                }

                if (entries != null)
                {
                    System.Diagnostics.Debug.WriteLine("Found " + entries.Count.ToString() + " when querying device " + client.ToString());
                    foreach (var entry in entries)
                    {
                        var cdpMatch = await MatchCDPEntry(dbContext, entry);
                        if (cdpMatch != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Device " + hostName + " identified as " + cdpMatch.Hostname + "." + cdpMatch.DomainName);
                            RegisteredDevices.Match((client as IPEndPoint).Address, cdpMatch);
                            TransferConfigurationToDevice((client as IPEndPoint).Address);
                            return true;
                        }
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine("Failed to identify device");
            return false;
        }
    }
}
