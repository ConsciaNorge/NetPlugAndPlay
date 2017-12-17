using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LibDHCPServer;
using System.Net;
using LibDHCPServer.Enums;
using Microsoft.Extensions.Configuration;
using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;
using LibDHCPServer.Utility.RemoteAgentIdFormats;
using LibDHCPServer.VolatilePool;
using Serilog;
using System.Text.RegularExpressions;
using NetPlugAndPlay.Services.DeviceConfigurator;

namespace NetPlugAndPlay.Services.DHCPServer
{
    public class Server
    {
        public Func<object, IPReleasedEventArgs, Task> OnIPReleased;

        private static Server s_instance = null;
        LibDHCPServer.DHCPServer _dhcpServer;
        DHCPPoolManager PoolManager { get; set; } = new DHCPPoolManager();

        public static string BootFilename
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:DHCP")["BootFilename"]; }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        public static string ClientIDFilter
        {
            get
            {
                try { return Startup.Configuration.GetSection("AppConfiguration:DHCP")["ClientIDFilter"]; }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return string.Empty;
            }
        }

        public static TimeSpan LeaseDuration
        {
            get
            {
                try { return TimeSpan.FromSeconds(Startup.Configuration.GetValue<int>("AppConfiguration:DHCP:LeaseDuration")); }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return TimeSpan.Zero;
            }
        }

        public static TimeSpan RequestTimeOut
        {
            get
            {
                try { return TimeSpan.FromSeconds(Startup.Configuration.GetValue<int>("AppConfiguration:DHCP:RequestTimeOut")); }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return TimeSpan.Zero;
            }
        }

        public static int MaxIncompleteRequests
        {
            get
            {
                try { return Startup.Configuration.GetValue<int>("AppConfiguration:DHCP:MaxIncompleteRequests"); }
                catch (Exception e) { Log.Logger.Here().Error(e, "Premature use of application configuration"); }
                return 10;
            }
        }

        public Server()
        {
            Log.Logger.Here().Information("Starting DHCP Server");
            if (s_instance != null)
                throw new Exception("Only a single instance of DHCP Server can be instantiated at a time");

            s_instance = this;

            _dhcpServer = new LibDHCPServer.DHCPServer();
            _dhcpServer.OnDHCPDiscover += new LibDHCPServer.DHCPServer.DHCPProcessDelegate(async (discovery, localEndPoint, remoteEndPoint) =>
            {
                return await GenerateLease(discovery, localEndPoint, remoteEndPoint);
            });

            _dhcpServer.OnDHCPRequest += new LibDHCPServer.DHCPServer.DHCPProcessDelegate(async (discovery, localEndPoint, remoteEndPoint) =>
            {
                return await GenerateLease(discovery, localEndPoint, remoteEndPoint);
            });
            _dhcpServer.OnDHCPRelease += new LibDHCPServer.DHCPServer.DHCPProcessDelegate(async (packet, localEndPoint, remoteEndPoint) =>
            {
                return await ReleaseLease(packet, localEndPoint, remoteEndPoint);
            });

            Task.Factory.StartNew(async () => { await _dhcpServer.Start(); });
        }

        internal Task RegisterNewPool(object sender, NewDHCPPoolEventArgs ev)
        {
            return Task.Run(() => {
                Log.Logger.Here().Information("Registering DHCP pool for  " + ev.Pool.Network.ToString());
                PoolManager.RegisterPool(ev.Pool);
                Log.Logger.Here().Information("Registered DHCP pool for  " + ev.Pool.Network.ToString());
            });
        }

        private async Task<DHCPPacketView> ReleaseLease(DHCPPacketView packet, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Log.Logger.Here().Information("DHCP release packet received from " + remoteEndPoint.ToString() + " for client IP " + packet.ClientIP);
            if(!DHCPClients.ReleaseClient(packet, localEndPoint, remoteEndPoint))
                Log.Logger.Here().Debug("No known client registered for client : " + packet.ClientId.ToString());

            lock (PoolManager)
            {
                if (!PoolManager.ReleaseLease(packet, localEndPoint, remoteEndPoint))
                {
                    Log.Logger.Here().Error("Failed to process DHCP release packet for client ID " + packet.ClientId.ToString() + " received from " + remoteEndPoint.ToString());
                    return null;
                }
            }

            if (OnIPReleased != null)
            {
                Delegate[] invocationList = OnIPReleased.GetInvocationList();
                Task[] onForgetIPAddressTasks = new Task[invocationList.Length];

                for (int i = 0; i < invocationList.Length; i++)
                {
                    Log.Logger.Here().Debug("DHCP Release informing other processes that " + packet.ClientIP.ToString() + " has been released");
                    try
                    {
                        onForgetIPAddressTasks[i] = ((Func<object, IPReleasedEventArgs, Task>)invocationList[i])(this, new IPReleasedEventArgs { Address = packet.ClientIP });
                    }
                    catch(Exception e)
                    {
                        Log.Logger.Here().Error(e, "Exception thrown while informing other tasks of DHCP released IP " + packet.ClientIP.ToString());
                    }
                }

                await Task.WhenAll(onForgetIPAddressTasks);
            }

            // DHCP Release does not appear to require an ACK.
            return null;
        }

        public static List<IPAddress> DNSServers
        {
            get
            {
                var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
                var dnsServers = adapters.Select(x => x.GetIPProperties()).Select(x => x.DnsAddresses).SelectMany(x => x).Distinct().Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();

                return dnsServers;
            }
        }

        private static bool dhcpInitialized = false;

        private void InitializeDHCPPools(IPAddress tftpServerAddress)
        {
            // TODO : Make this part of DeviceConfigurator
            if (dhcpInitialized)
                return;

            string connectionString;

            try
            {
                connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            }
            catch(Exception e)
            {
                Log.Logger.Here().Error(e, "DHCP server attempted to use configuration settings prematurely");
                return;
            }

            var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
            dbOptions.UseSqlServer(connectionString);
            var dbContext = new PnPServerContext(dbOptions.Options);

            var devicesWithPools = dbContext.NetworkDevices.Where(x => x.DHCPRelay).Include("DHCPExclusions").ToList();

            foreach(var device in devicesWithPools)
            {
                var prefix = NetworkPrefix.Parse(device.Network);

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
                Log.Logger.Here().Debug("Lease duration : " + LeaseDuration.ToString());
                Log.Logger.Here().Debug("Request time out : " + RequestTimeOut.ToString());
                Log.Logger.Here().Debug("Maximum incomplete requests : " + MaxIncompleteRequests.ToString());

                PoolManager.RegisterPool(new DhcpPool
                {
                    Network = prefix,
                    DefaultGateways = new List<IPAddress> { IPAddress.Parse(device.IPAddress) },
                    Exlusions = device.DHCPExclusions
                        .Select(x =>
                            new IPRange {
                                Start = IPAddress.Parse(x.Start),
                                End = IPAddress.Parse(x.End)
                            }
                        )
                        .ToList(),
                    LeaseDuration = LeaseDuration,
                    RequestTimeOut = RequestTimeOut,
                    MaxIncompleteRequests = MaxIncompleteRequests,
                    PoolOptions = new LeaseOptions
                    {
                        DomainName = device.DomainName,
                        TFTPServers = new List<string>
                        {
                            tftpServerAddress.ToString()
                        },
                        BootFile = device.DHCPTftpBootfile,
                        DNSServers = DNSServers
                    }
                });
            }

            dhcpInitialized = true;
            return;
        }

        private Lease GenerateLeaseFromPool(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            if (request.ClientId == null)
            {
                Log.Logger.Here().Information("DHCP packet recieved from " + remoteEndPoint.ToString() + " without a Client ID option. Ignoring - client MAC : " + request.ClientHardwareAddress.ToString());
                return null;
            }

            var clientIDFilter = new Regex(ClientIDFilter);
            if(!clientIDFilter.IsMatch(request.ClientId.ToString()))
            {
                Log.Logger.Here().Information("DHCP packet received from " + remoteEndPoint.ToString() + " with client ID " + request.ClientId.ToString() + " but does not match filter \"" + ClientIDFilter + "\". Ignoring");
                return null;
            }

            if (request.RelayAgentIP == null)
                Log.Logger.Here().Debug("Generating lease for request for client-id : " + request.ClientId.ToString() + " received from : " + remoteEndPoint.ToString());
            else
                Log.Logger.Here().Debug("Generating lease for request for client-id : " + request.ClientId.ToString() + " received from : " + remoteEndPoint.ToString() + " via relay : " + request.RelayAgentIP.ToString());


            Lease lease = null;
            lock(PoolManager)
            {
                try
                {
                    lease = PoolManager.ReserveByRelayAddress(remoteEndPoint, request.ClientId, request.TransactionId);
                }
                catch(Exception e)
                {
                    Log.Logger.Here().Error(e, "Failed to get lease");
                    return null;
                }
            }

            if(lease == null)
            {
                Log.Logger.Here().Warning("Pool manager could not find an available IP for request " + request.TransactionId.ToString("X8") + " from relay " + remoteEndPoint.ToString());
                return null;
            }

            return lease;
        }

        private DHCPPacketView CreateDHCPResponse(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, DHCPClient knownClient)
        {
            // TODO : Extend DHCP packet view to provide a good ToString() and refactor logging

            Log.Logger.Here().Debug("Creating DHCP response for " + request.DHCPMessageType.ToString() + " for client ID " + request.ClientId.ToString() + " received from " + remoteEndPoint.ToString());
            DHCPPacketView result = null;
            switch(request.DHCPMessageType)
            {
                case DHCPMessageType.DHCPDISCOVER:
                    result = new DHCPPacketView(DHCPMessageType.DHCPOFFER);
                    break;
                case DHCPMessageType.DHCPREQUEST:
                    result = new DHCPPacketView(DHCPMessageType.DHCPACK);
                    break;
                default:
                    Log.Logger.Here().Error("Unknown source DHCP message type " + request.DHCPMessageType.ToString());
                    return null;
            }
            Log.Logger.Here().Debug("DHCP response message type " + result.DHCPMessageType.ToString());

            result.YourIP = knownClient.DHCPLease.Address;
            Log.Logger.Here().Debug("Your IP " + result.YourIP.ToString());

            result.SubnetMask = knownClient.DHCPLease.Pool.Network.SubnetMask;
            Log.Logger.Here().Debug("Subnet mask " + result.SubnetMask.ToString());

            result.Routers = knownClient.DHCPLease.Pool.DefaultGateways;
            Log.Logger.Here().Debug("Routers " + string.Join(",", result.Routers.Select(x => "(" + x.ToString() + ")").ToList()));

            if (knownClient.DHCPLease.Options.DNSServers != null && knownClient.DHCPLease.Options.DNSServers.Count > 0)
            {
                result.DomainNameServers = knownClient.DHCPLease.Options.DNSServers;
                Log.Logger.Here().Debug("DNS servers" + string.Join(",", result.DomainNameServers.Select(x => "(" + x.ToString() + ")").ToList()));
            }

            if (!string.IsNullOrEmpty(knownClient.DHCPLease.Options.Hostname))
            {
                result.Hostname = knownClient.DHCPLease.Options.Hostname;
                Log.Logger.Here().Debug("Hostname " + result.Hostname);
            }

            if (!string.IsNullOrEmpty(knownClient.DHCPLease.Options.DomainName))
            {
                result.DomainName = knownClient.DHCPLease.Options.DomainName;
                Log.Logger.Here().Debug("Domain name " + result.DomainName);
            }

            if (!string.IsNullOrEmpty(knownClient.DHCPLease.Options.BootFile))
            {
                if (request.ParameterList.Contains(DHCPOptionType.BootfileName))
                {
                    result.TFTPBootfile = knownClient.DHCPLease.Options.BootFile;
                    Log.Logger.Here().Debug("Option 67 TFTP Boot file" + result.TFTPBootfile);
                }
                else
                {
                    result.BootFile = knownClient.DHCPLease.Options.BootFile;
                    Log.Logger.Here().Debug("Bootp header bootfile " + result.BootFile);
                }

                if (request.ParameterList.Contains(DHCPOptionType.TFTPserveraddress))
                {
                    result.TFTPServer150 = new List<IPAddress> { localEndPoint.Address };
                    Log.Logger.Here().Debug("Option 150 - TFTP Servers " + string.Join(",", result.TFTPServer150.Select(x => x.ToString()).ToList()));
                }
                else if (request.ParameterList.Contains(DHCPOptionType.ServerName))
                {
                    result.TFTPServer = localEndPoint.Address.ToString();
                    Log.Logger.Here().Debug("Option 66 - TFTP Server " + result.TFTPServer);
                }
                else
                {
                    result.ServerHostname = localEndPoint.Address.ToString();
                    Log.Logger.Here().Debug("Bootp header sname (TFTP server) " + result.ServerHostname);
                }
            }

            result.RenewalTimeValue = knownClient.DHCPLease.Pool.LeaseDuration.Multiply(0.50);
            Log.Logger.Here().Debug("Renewal time " + result.RenewalTimeValue.ToString());

            result.RebindingTimeValue = knownClient.DHCPLease.Pool.LeaseDuration.Multiply(0.90);
            Log.Logger.Here().Debug("Rebinding time " + result.RebindingTimeValue.ToString());

            result.IPAddressLeaseTime = knownClient.DHCPLease.Pool.LeaseDuration;
            Log.Logger.Here().Debug("Lease time " + result.IPAddressLeaseTime.ToString());

            result.ClientHardwareAddress = request.ClientHardwareAddress;
            Log.Logger.Here().Debug("Client hardware address " + result.ClientHardwareAddress.ToString());

            result.TransactionId = request.TransactionId;
            Log.Logger.Here().Debug("Transaction ID " + result.TransactionId.ToString("X8"));

            result.TimeElapsed = request.TimeElapsed;
            Log.Logger.Here().Debug("Time elapsed " + result.TimeElapsed.ToString());

            result.BroadcastFlag = request.BroadcastFlag;
            Log.Logger.Here().Debug("Broadcast Flag " + result.BroadcastFlag.ToString());

            result.NextServerIP = localEndPoint.Address;
            Log.Logger.Here().Debug("Next server IP " + result.NextServerIP.ToString());

            result.RelayAgentIP = request.RelayAgentIP;
            Log.Logger.Here().Debug("Relay agent IP " + result.RelayAgentIP.ToString());

            result.Hops = request.Hops;
            Log.Logger.Here().Debug("Hops " + result.Hops.ToString());

            result.DHCPServerIdentifier = localEndPoint.Address;
            Log.Logger.Here().Debug("DHCP Server identifier " + result.DHCPServerIdentifier.ToString());

            return result;
        }

        private DHCPPacketView ProcessDHCPDiscover(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Log.Logger.Here().Debug("Received DHCP Discover message from client ID " + request.ClientId.ToString());

            InitializeDHCPPools(localEndPoint.Address);

            Log.Logger.Here().Debug("Looking for cached client for client ID " + request.ClientId.ToString());
            var knownClient = DHCPClients.FindKnownClient(request, localEndPoint, remoteEndPoint);
            if (knownClient == null)
            {
                Log.Logger.Here().Debug("Client " + request.ClientId.ToString() + " is unknown, generating a lease from a pool");
                var lease = GenerateLeaseFromPool(request, localEndPoint, remoteEndPoint);
                if (lease == null)
                {
                    Log.Logger.Here().Warning("Failed to generate a lease from a pool for " + request.ClientId.ToString());
                    return null;
                }

                Log.Logger.Here().Debug("Registering a known client for " + request.ClientId.ToString() + " with for IP " + lease.Address.ToString());
                knownClient = new DHCPClient
                {
                    ClientId = request.ClientId,
                    DHCPLease = lease,
                    NetworkDeviceId = Guid.Empty
                };

                DHCPClients.Add(knownClient);
            }
            else
            {
                Log.Logger.Here().Debug("Client " + request.ClientId + " is known and the request lease timers are being reset");
                DHCPClients.ResetLeaseTimers(knownClient);
            }

            Log.Logger.Here().Debug("Generating response packet for discover for client id " + request.ClientId.ToString());
            return CreateDHCPResponse(request, localEndPoint, remoteEndPoint, knownClient);
        }

        private DHCPPacketView ProcessDHCPRequest(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Log.Logger.Here().Debug("Received DHCP Request message from client ID " + request.ClientId.ToString());

            var knownClient = DHCPClients.FindKnownClient(request, localEndPoint, remoteEndPoint);
            if (knownClient == null)
            {
                Log.Logger.Here().Debug("Could not find known client for " + request.ClientId.ToString() + " aborting");
                return null;
            }

            Log.Logger.Here().Debug("Updating lease timers for client " + request.ClientId.ToString() + " to retain address " + knownClient.DHCPLease.Address.ToString());
            DHCPClients.SetLeaseTimers(knownClient);

            Log.Logger.Here().Debug("Generating response packet for request for client id " + request.ClientId.ToString());
            return CreateDHCPResponse(request, localEndPoint, remoteEndPoint, knownClient);
        }

        static private async Task<DHCPPacketView> GenerateLease(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Log.Logger.Here().Debug("(static) DHCP packet received from " + remoteEndPoint.ToString());
            // TODO : Get rid of the next line
            await Task.Delay(1);
            switch (request.DHCPMessageType)
            {
                case DHCPMessageType.DHCPDISCOVER:
                    return s_instance.ProcessDHCPDiscover(request, localEndPoint, remoteEndPoint);
                case DHCPMessageType.DHCPREQUEST:
                    return s_instance.ProcessDHCPRequest(request, localEndPoint, remoteEndPoint);
                default:
                    return null;
            }
        }
    }
}
