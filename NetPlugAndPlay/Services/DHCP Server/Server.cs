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

namespace NetPlugAndPlay.Services.DHCP_Server
{
    public class Server
    {
        private static Server s_instance = null;
        DHCPServer _dhcpServer;

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

        static private async Task<DHCPPacketView> GenerateLease(DHCPPacketView discovery, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            DHCPPacketView result;
            switch (discovery.DHCPMessageType)
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

            var connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
            dbOptions.UseSqlServer(connectionString);

            var dbContext = new PnPServerContext(dbOptions.Options);

            var record = await dbContext.NetworkDevices.ToListAsync();
            System.Diagnostics.Debug.WriteLine("db worked from DHCP " + record.Count);

            result.ClientHardwareAddress = discovery.ClientHardwareAddress;
            result.TransactionId = discovery.TransactionId;
            result.TimeElapsed = discovery.TimeElapsed;
            result.BroadcastFlag = discovery.BroadcastFlag;
            result.NextServerIP = localEndPoint.Address;
            result.RelayAgentIP = discovery.RelayAgentIP;
            result.Hops = discovery.Hops;

            result.RenewalTimeValue = TimeSpan.FromHours(12);
            result.RebindingTimeValue = TimeSpan.FromHours(21);
            result.IPAddressLeaseTime = TimeSpan.FromHours(24);

            result.DHCPServerIdentifier = localEndPoint.Address;

            result.YourIP = IPAddress.Parse("172.20.0.99");
            result.SubnetMask = IPAddress.Parse("255.255.255.0");
            result.Routers = new List<IPAddress> { IPAddress.Parse("172.20.0.1") };
            result.Hostname = "bob";
            result.DomainName = "minions.com";
            result.DomainNameServers = new List<IPAddress> { IPAddress.Parse("10.100.11.81") };

            return result;
        }
    }
}
