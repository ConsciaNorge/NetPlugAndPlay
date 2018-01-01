using LibDHCPServer.VolatilePool;
using libnetworkutility;
using libterminal;
using libterminal.Helpers.Model;
using libterminal.Helpers.Parsers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetPlugAndPlay.Models;
using NetPlugAndPlay.Services.Common.NetworkTools;
using NetPlugAndPlay.Services.DeviceConfigurator.ViewModelExtensions;
using NetPlugAndPlay.Services.DeviceConfigurator.ViewModels;
using NetPlugAndPlay.Services.DHCPServer;
using NetPlugAndPlay.Services.SyslogServer;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DeviceConfigurator
{
    public partial class DeviceConfigurator
    {
        public static DeviceConfigurator Instance { get; set; }

        public DeviceConfigurator()
        {
            if (Instance != null)
                throw new Exception("Only one instance of DeviceConfigurator is permitted");

            Instance = this;
        }

        public static string DeviceConfiguredLogMessage
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:Automation")["DeviceConfiguredLogMessage"]; }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        public static string TelnetUsername
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:TelnetControl")["Username"]; }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        public static string TelnetPassword
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:TelnetControl")["Password"]; }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        public static string TelnetEnablePassword
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:TelnetControl")["EnablePassword"]; }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
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
            Log.Logger.Here().Information("Attempting to identify what is connected to [" + cdpEntry.DeviceID + "] interface [" + cdpEntry.PortId + "]");
            Log.Logger.Here().Debug("Looking for " + cdpEntry.DeviceID);
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
                Log.Logger.Here().Debug(cdpEntry.DeviceID + " not found in the configuration database");
                return null;
            }

            Log.Logger.Here().Debug("Looking for " + cdpEntry.PortId);
            var remotePortId = GetPhysicalInterfaceName(cdpEntry.PortId.ToLowerInvariant());
            Log.Logger.Here().Debug("Looking for " + remotePortId + " (mutated for search)");

            var remoteInterface = neighborDevice.DeviceType.Interfaces
                .Where(x =>
                    x.Name.ToLowerInvariant() == remotePortId
                )
                .FirstOrDefault();

            if (remoteInterface == null)
            {
                Log.Logger.Here().Debug(cdpEntry.PortId + " not found in the configuration database for the device type " + neighborDevice.DeviceType.ProductId);
                return null;
            }
            Log.Logger.Here().Debug(cdpEntry.PortId + " on device type " + neighborDevice.DeviceType.ProductId + " is SNMP index " + remoteInterface.InterfaceIndex + " resolving uplink");

            var uplink = await dbContext.NetworkDeviceLinks
                .Where(x =>
                    x.ConnectedToDevice.Id == neighborDevice.Id &&
                    x.ConnectedToInterfaceIndex == remoteInterface.InterfaceIndex
                )
                .Include("NetworkDevice")
                .FirstOrDefaultAsync();

            if (uplink == null)
            {
                Log.Logger.Here().Debug(cdpEntry.DeviceID + " on interface " + cdpEntry.PortId + " could not be located in the configuration database");
                return null;
            }

            Log.Logger.Here().Information(cdpEntry.DeviceID + " on interface " + cdpEntry.PortId + " is connected to " + uplink.NetworkDevice.Hostname + "." + uplink.NetworkDevice.DomainName);
            return uplink.NetworkDevice;
        }

        internal Task ForgetIP(object sender, IPReleasedEventArgs ev)
        {
            return Task.Run(() => {
                Log.Logger.Here().Information("Unregistering device with IP " + ev.Address.ToString() + " from device manager");
                if (RegisteredDevices.UnregisterDevice(ev.Address))
                    Log.Logger.Here().Information("Device with IP " + ev.Address.ToString() + " successfully unregistered from device manager");
                else
                    Log.Logger.Here().Warning("Device with IP " + ev.Address.ToString() + " either was not registered with device manager or could not be unregistered");

                if (ConnectionManager.Instance.RemoveConnectionsByHost(ev.Address.ToString()))
                    Log.Logger.Here().Information("Closed telnet and ssh connections to " + ev.Address.ToString());
                else
                    Log.Logger.Here().Debug("No telnet or ssh connections were closed to " + ev.Address.ToString());
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
            Log.Logger.Here().Information("Copied configuration to " + address.ToString());
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
                Log.Logger.Here().Error(e, "Failed to get database connection string");
                return;
            }

            if (!args.Message.Contains(DeviceConfiguredLogMessage))
                return;

            Log.Logger.Here().Information("Device : " + args.Host.ToString() + " signaled it is ready to be identified");

            if (!await IdentifyDevice(args.Host))
            {
                Log.Logger.Here().Information("Failed to identify device : " + args.Host.ToString() + " as CDP neighbors either cannot be resolved or are unknown.");
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
                Log.Logger.Here().Error(e, "Failed to get database connection string");
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
                Log.Logger.Here().Error("Received null or whitespace result to 'show cdp entries *'");
            else
            {
                Log.Logger.Here().Debug("Received CDP entries\n" + entriesText);
                var parser = new ShowCDPEntry();
                List<ShowCDPEntryItem> entries = null;
                try
                {
                    entries = parser.Parse(entriesText);
                }
                catch (Exception e)
                {
                    Log.Logger.Here().Error(e, "Failed to parse CDP entries");
                    return false;
                }

                if (entries != null)
                {
                    Log.Logger.Here().Information("Found " + entries.Count.ToString() + " when querying device " + client.ToString());
                    foreach (var entry in entries)
                    {
                        var cdpMatch = await MatchCDPEntry(dbContext, entry);
                        if (cdpMatch != null)
                        {
                            Log.Logger.Here().Information("Device " + hostName + " identified as " + cdpMatch.Hostname + "." + cdpMatch.DomainName);
                            RegisteredDevices.Match((client as IPEndPoint).Address, cdpMatch);
                            TransferConfigurationToDevice((client as IPEndPoint).Address);
                            return true;
                        }
                    }
                }
            }

            Log.Logger.Here().Information("Failed to identify device " + client.ToString());
            return false;
        }

        public Func<object, NewDHCPPoolEventArgs, Task> OnRegisterNewDHCPPool;
        private async Task SignalNewDHCPPool(DhcpPool pool)
        {
            if (OnRegisterNewDHCPPool == null)
                return;

            Delegate[] invocationList = OnRegisterNewDHCPPool.GetInvocationList();
            Task[] onRegisterNewDHCPPoolTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
            {
                onRegisterNewDHCPPoolTasks[i] = (
                    (Func<object, NewDHCPPoolEventArgs, Task>)invocationList[i]
                )(
                    this,
                    new NewDHCPPoolEventArgs
                    {
                        Pool = pool
                    }
                );
            }

            await Task.WhenAll(onRegisterNewDHCPPoolTasks);
        }

        public Func<object, ReleaseDHCPPoolEventArgs, Task> OnReleaseDHCPPool;
        private async Task SignalRemoveDHCPPoolForNetwork(NetworkPrefix network)
        {
            if (OnReleaseDHCPPool == null)
                return;

            Delegate[] invocationList = OnReleaseDHCPPool.GetInvocationList();
            Task[] onReleaseDHCPPoolTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
            {
                onReleaseDHCPPoolTasks[i] = (
                    (Func<object, ReleaseDHCPPoolEventArgs, Task>)invocationList[i]
                )(
                    this,
                    new ReleaseDHCPPoolEventArgs
                    {
                        Network = network
                    }
                );
            }

            await Task.WhenAll(onReleaseDHCPPoolTasks);
        }

        public static async Task NetworkDeviceAdded(NetworkDevice device)
        {
            Log.Logger.Here().Information("Network device " + device.Hostname + "." + device.DomainName + " added");

            if (device.DHCPRelay)
            {
                var prefix = NetworkPrefix.Parse(device.Network);

                var tftpServerAddress = LocalRoutingTable.QueryRoutingInterface(IPAddress.Parse(device.IPAddress));

                Log.Logger.Here().Information("Registering new DHCP pool for " + prefix.ToString());
                Log.Logger.Here().Debug("Exclusions : " + string.Join(",",
                    device.DHCPExclusions
                        .Select(x =>
                            (x.Start.ToString() + "-" + x.End.ToString())
                        )
                        .ToList()
                    )
                );
                Log.Logger.Here().Debug("Default gateways : (" + device.IPAddress + ")");
                Log.Logger.Here().Debug("TFTP Server : " + tftpServerAddress.ToString());
                Log.Logger.Here().Debug("Domain name : " + device.DomainName);
                Log.Logger.Here().Debug("Boot filename : " + device.DHCPTftpBootfile);
                Log.Logger.Here().Debug("Lease duration : " + Server.LeaseDuration.ToString());
                Log.Logger.Here().Debug("Request time out : " + Server.RequestTimeOut.ToString());
                Log.Logger.Here().Debug("Maximum incomplete requests : " + Server.MaxIncompleteRequests.ToString());

                var pool = new DhcpPool
                {
                    Network = prefix,
                    DefaultGateways = new List<IPAddress> { IPAddress.Parse(device.IPAddress) },
                    Exclusions = new IPRanges(device.DHCPExclusions
                        .Select(x =>
                            new IPRange
                            {
                                Start = IPAddress.Parse(x.Start),
                                End = IPAddress.Parse(x.End)
                            }
                        )
                        .ToList(),
                        true
                    ),
                    LeaseDuration = Server.LeaseDuration,
                    RequestTimeOut = Server.RequestTimeOut,
                    MaxIncompleteRequests = Server.MaxIncompleteRequests,
                    PoolOptions = new LeaseOptions
                    {
                        DomainName = device.DomainName,
                        TFTPServers = new List<string>
                            {
                                tftpServerAddress.ToString()
                            },
                        BootFile = device.DHCPTftpBootfile,
                        DNSServers = Server.DNSServers
                    }
                };

                await Instance.SignalNewDHCPPool(pool);
            }
        }

        public static async Task NetworkDeviceChanged(NetworkDeviceChanges changes)
        {
            Log.Logger.Here().Information("Network device " + changes.Old.Hostname + "." + changes.Old.DomainName + " changed");
            await Task.Delay(1);
        }

        public static async Task NetworkDeviceRemoved(NetworkDevice networkDevice)
        {
            Log.Logger.Here().Information("Network device " + networkDevice.Hostname + "." + networkDevice.DomainName + " changed");
            await Task.Delay(1);
        }

        public Func<object, ChangeDHCPPoolEventArgs, Task> OnChangeDHCPPool;
        private async Task SignalChangeDHCPPoolForNetwork(DHCPPoolChangeViewModel changes)
        {
            if (OnChangeDHCPPool == null)
                return;

            Delegate[] invocationList = OnChangeDHCPPool.GetInvocationList();
            Task[] onChangeDHCPPoolTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
            {
                onChangeDHCPPoolTasks[i] = (
                    (Func<object, ChangeDHCPPoolEventArgs, Task>)invocationList[i]
                )(
                    this,
                    new ChangeDHCPPoolEventArgs
                    {
                        Changes = changes
                    }
                );
            }

            await Task.WhenAll(onChangeDHCPPoolTasks);
        }

        internal static async Task ProcessDHCPChanges(DHCPPoolChangeViewModel dhcpChanges)
        {
            if (!dhcpChanges.Changed())
                return;

            await Instance.SignalChangeDHCPPoolForNetwork(dhcpChanges);
        }
    }
}
