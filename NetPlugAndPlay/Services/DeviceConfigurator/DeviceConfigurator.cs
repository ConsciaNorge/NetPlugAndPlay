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
using Serilog;
using NetPlugAndPlay.Services.Common.NetworkTools;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public partial class DeviceConfigurator
    {
        public static string DeviceConfiguredLogMessage
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:Automation")["DeviceConfiguredLogMessage"]; }
                catch (Exception e) { Log.Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        string TelnetUsername
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:TelnetControl")["Username"]; }
                catch (Exception e) { Log.Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        string TelnetPassword
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:TelnetControl")["Password"]; }
                catch (Exception e) { Log.Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        string TelnetEnablePassword
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:TelnetControl")["EnablePassword"]; }
                catch (Exception e) { Log.Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

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
            Log.Information("Attempting to identify what is connected to [" + cdpEntry.DeviceID + "] interface [" + cdpEntry.PortId + "]");
            Log.Debug("Looking for " + cdpEntry.DeviceID);
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
            {
                Log.Debug(cdpEntry.DeviceID + " not found in the configuration database");
                return null;
            }

            Log.Debug("Looking for " + cdpEntry.PortId);
            var remotePortId = GetPhysicalInterfaceName(cdpEntry.PortId.ToLowerInvariant());
            Log.Debug("Looking for " + remotePortId + " (mutated for search)");

            var remoteInterface = neighborDevice.DeviceType.Interfaces
                .Where(x =>
                    x.Name.ToLowerInvariant() == remotePortId
                )
                .FirstOrDefault();

            if (remoteInterface == null)
            {
                Log.Debug(cdpEntry.PortId + " not found in the configuration database for the device type " + neighborDevice.DeviceType.ProductId);
                return null;
            }
            Log.Debug(cdpEntry.PortId + " on device type " + neighborDevice.DeviceType.ProductId + " is SNMP index " + remoteInterface.InterfaceIndex + " resolving uplink");

            var uplink = await dbContext.NetworkDeviceLinks
                .Where(x =>
                    x.ConnectedToDevice.Id == neighborDevice.Id &&
                    x.ConnectedToInterfaceIndex == remoteInterface.InterfaceIndex
                )
                .Include("NetworkDevice")
                .FirstOrDefaultAsync();

            if (uplink == null)
            {
                Log.Debug(cdpEntry.DeviceID + " on interface " + cdpEntry.PortId + " could not be located in the configuration database");
                return null;
            }

            Log.Information(cdpEntry.DeviceID + " on interface " + cdpEntry.PortId + " is connected to " + uplink.NetworkDevice.Hostname + "." + uplink.NetworkDevice.DomainName);
            return uplink.NetworkDevice;
        }

        internal Task ForgetIP(object sender, IPReleasedEventArgs ev)
        {
            return Task.Run(() => {
                Log.Information("Unregistering device with IP " + ev.Address.ToString() + " from device manager");
                if (RegisteredDevices.UnregisterDevice(ev.Address))
                    Log.Information("Device with IP " + ev.Address.ToString() + " successfully unregistered from device manager");
                else
                    Log.Warning("Device with IP " + ev.Address.ToString() + " either was not registered with device manager or could not be unregistered");

                if (ConnectionManager.Instance.RemoveConnectionsByHost(ev.Address.ToString()))
                    Log.Information("Closed telnet and ssh connections to " + ev.Address.ToString());
                else
                    Log.Debug("No telnet or ssh connections were closed to " + ev.Address.ToString());
            });
        }

        void TransferConfigurationToDevice(IPAddress address)
        {
            var localAddress = LocalRoutingTable.QueryRoutingInterface(address);
            var userInfo = TelnetUsername + ":" + TelnetPassword;

            // TODO : AppSettings
            var uri = new Uri("telnet://" + userInfo + "@" + address.ToString());
            var copyResult = libterminal.Helpers.TaskCopy.Run(
                uri,
                TelnetEnablePassword,
                "tftp://" + localAddress.ToString() + "/config.txt",
                "running-config"
            );
            Log.Information("Copied configuration to " + address.ToString());
        }

        public async Task SyslogMessageHandler(object sender, SyslogMessageEventArgs args)
        {
            string connectionString;
            try
            {
                // TODO : Make sure that this code doesn't run until the startup is complete
                connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            }
            catch(Exception e)
            {
                Log.Error(e, "Failed to get database connection string");
                return;
            }

            if (!args.Message.Contains(DeviceConfiguredLogMessage))
                return;

            Log.Information("Device : " + args.Host.ToString() + " signaled it is ready to be identified");

            if (!await IdentifyDevice(args.Host))
            {
                Log.Information("Failed to identify device : " + args.Host.ToString() + " as CDP neighbors either cannot be resolved or are unknown.");
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
            catch(Exception e)
            {
                Log.Error(e, "Failed to get database connection string");
                return false;
            }

            var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
            dbOptions.UseSqlServer(connectionString);

            var dbContext = new PnPServerContext(dbOptions.Options);

            var hostName = string.Empty;
            if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                hostName = (client as IPEndPoint).Address.ToString();
            else
                throw new Exception("Address family not supported");

            var userInfo = TelnetUsername + ":" + TelnetPassword;
            var uri = new Uri("telnet://" + userInfo + "@" + hostName);

            var entriesText = libterminal.Helpers.TaskShowCDPEntries.Run(
                uri,
                TelnetEnablePassword
            );

            if (string.IsNullOrWhiteSpace(entriesText))
                Log.Error("Received null or whitespace result to 'show cdp entries *'\n" + entriesText);
            else
            {
                Log.Debug("Received CDP entries");
                var parser = new ShowCDPEntry();
                List<ShowCDPEntryItem> entries = null;
                try
                {
                    entries = parser.Parse(entriesText);
                }
                catch (Exception e)
                {
                    Log.Error(e, "Failed to parse CDP entries");
                    return false;
                }

                if (entries != null)
                {
                    Log.Information("Found " + entries.Count.ToString() + " when querying device " + client.ToString());
                    foreach (var entry in entries)
                    {
                        var cdpMatch = await MatchCDPEntry(dbContext, entry);
                        if (cdpMatch != null)
                        {
                            Log.Information("Device " + hostName + " identified as " + cdpMatch.Hostname + "." + cdpMatch.DomainName);
                            RegisteredDevices.Match((client as IPEndPoint).Address, cdpMatch);
                            TransferConfigurationToDevice((client as IPEndPoint).Address);
                            return true;
                        }
                    }
                }
            }

            Log.Information("Failed to identify device " + client.ToString());
            return false;
        }
    }
}
