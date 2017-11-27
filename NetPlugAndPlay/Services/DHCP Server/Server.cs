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

namespace NetPlugAndPlay.Services.DHCP_Server
{
    public class Server
    {
        private static Server s_instance = null;
        DHCPServer _dhcpServer;
        DHCPPoolManager PoolManager { get; set; } = new DHCPPoolManager();
        public Server()
        {
            if (s_instance != null)
            {
                throw new Exception("Only a single instance of DHCP Server can be instantiated at a time");
            }
            s_instance = this;

            _dhcpServer = new DHCPServer();
            _dhcpServer.OnDHCPDiscover += new DHCPServer.DHCPProcessDelegate(async (discovery, localEndPoint, remoteEndPoint) =>
            {
                return await GenerateLease(discovery, localEndPoint, remoteEndPoint);
            });

            _dhcpServer.OnDHCPRequest += new DHCPServer.DHCPProcessDelegate(async (discovery, localEndPoint, remoteEndPoint) =>
            {
                return await GenerateLease(discovery, localEndPoint, remoteEndPoint);
            });

            Task.Factory.StartNew(async () => { await _dhcpServer.Start(); });
        }

        private List<IPAddress> GetDNSServers()
        {
            var adapters = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();
            var dnsServers = adapters.Select(x => x.GetIPProperties()).Select(x => x.DnsAddresses).SelectMany(x => x).Distinct().Where(x => x.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).ToList();

            return dnsServers;
        }

        private static bool dhcpInitialized = false;

        private void InitializeDHCPPools(IPAddress tftpServerAddress)
        {
            if (dhcpInitialized)
                return;

            var connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
            dbOptions.UseSqlServer(connectionString);

            var dbContext = new PnPServerContext(dbOptions.Options);

            var devicesWithPools = dbContext.NetworkDevices.Where(x => x.DHCPRelay).Include("DHCPExclusions").ToList();

            foreach(var device in devicesWithPools)
            {
                var prefix = NetworkPrefix.Parse(device.Network);

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
                    LeaseDuration = TimeSpan.FromMinutes(5),
                    RequestTimeOut = TimeSpan.FromSeconds(3),
                    MaxIncompleteRequests = 10,
                    PoolOptions = new LeaseOptions
                    {
                        DomainName = device.DomainName,
                        TFTPServers = new List<string>
                        {
                            tftpServerAddress.ToString()
                        },
                        BootFile = "unprovisioned.config.txt",
                        DNSServers = GetDNSServers()
                    }
                });
            }

            dhcpInitialized = true;
            return;
        }

        private Lease GenerateLeaseFromPool(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            Lease lease = null;
            lock(PoolManager)
            {
                try
                {
                    lease = PoolManager.ReserveByRelayAddress(remoteEndPoint, request.ClientId.GetBytes(), request.TransactionId);
                }
                catch(Exception e)
                {
                    System.Diagnostics.Debug.WriteLine("Failed to get lease : " + e.Message);
                    return null;
                }
            }

            if(lease == null)
            {
                System.Diagnostics.Debug.WriteLine("Pool manager could not find an available IP for request " + request.TransactionId.ToString("X8") + " from relay " + remoteEndPoint.ToString());
                return null;
            }

            return lease;
        }

        private DHCPPacketView CreateDHCPResponse(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, DHCPClient knownClient)
        {
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
                    return null;
            }

            result.YourIP = knownClient.DHCPLease.Address;
            result.SubnetMask = knownClient.DHCPLease.Pool.Network.SubnetMask;
            result.Routers = knownClient.DHCPLease.Pool.DefaultGateways;
            if (knownClient.DHCPLease.Options.DNSServers != null && knownClient.DHCPLease.Options.DNSServers.Count > 0)
                result.DomainNameServers = knownClient.DHCPLease.Options.DNSServers;

            if (!string.IsNullOrEmpty(knownClient.DHCPLease.Options.Hostname))
                result.Hostname = knownClient.DHCPLease.Options.Hostname;

            if (!string.IsNullOrEmpty(knownClient.DHCPLease.Options.DomainName))
                result.DomainName = knownClient.DHCPLease.Options.DomainName;

            if (!string.IsNullOrEmpty(knownClient.DHCPLease.Options.BootFile))
            {
                if (request.ParameterList.Contains(DHCPOptionType.BootfileName))
                    result.TFTPBootfile = knownClient.DHCPLease.Options.BootFile;
                else
                    result.BootFile = knownClient.DHCPLease.Options.BootFile;

                if (request.ParameterList.Contains(DHCPOptionType.TFTPserveraddress))
                    result.TFTPServer150 = new List<IPAddress> { localEndPoint.Address };
                else if (request.ParameterList.Contains(DHCPOptionType.ServerName))
                    result.TFTPServer = localEndPoint.Address.ToString();
                else
                    result.ServerHostname = localEndPoint.Address.ToString();
            }

            result.RenewalTimeValue = knownClient.DHCPLease.Pool.LeaseDuration.Multiply(0.50);
            result.RebindingTimeValue = knownClient.DHCPLease.Pool.LeaseDuration.Multiply(0.90);
            result.IPAddressLeaseTime = knownClient.DHCPLease.Pool.LeaseDuration;

            result.ClientHardwareAddress = request.ClientHardwareAddress;
            result.TransactionId = request.TransactionId;
            result.TimeElapsed = request.TimeElapsed;
            result.BroadcastFlag = request.BroadcastFlag;
            result.NextServerIP = localEndPoint.Address;
            result.RelayAgentIP = request.RelayAgentIP;
            result.Hops = request.Hops;

            result.DHCPServerIdentifier = localEndPoint.Address;

            return result;
        }

        private DHCPPacketView ProcessDHCPDiscover(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            InitializeDHCPPools(localEndPoint.Address);

            var knownClient = DHCPClients.FindKnownClient(request, localEndPoint, remoteEndPoint);
            if (knownClient == null)
            {
                var lease = GenerateLeaseFromPool(request, localEndPoint, remoteEndPoint);
                if (lease == null)
                    return null;

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
                DHCPClients.ResetLeaseTimers(knownClient);
            }

            return CreateDHCPResponse(request, localEndPoint, remoteEndPoint, knownClient);
        }

        private DHCPPacketView ProcessDHCPRequest(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            var knownClient = DHCPClients.FindKnownClient(request, localEndPoint, remoteEndPoint);
            if (knownClient == null)
                return null;

            DHCPClients.SetLeaseTimers(knownClient);

            return CreateDHCPResponse(request, localEndPoint, remoteEndPoint, knownClient);
        }

        static private async Task<DHCPPacketView> GenerateLease(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            System.Diagnostics.Debug.WriteLine("DHCP packet received from " + remoteEndPoint.ToString());
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
