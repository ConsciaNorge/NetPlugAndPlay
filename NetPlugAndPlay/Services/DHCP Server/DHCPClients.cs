using LibDHCPServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.DHCP_Server
{
    public class DHCPClients
    {
        private static List<DHCPClient> KnownClients { get; set; } = new List<DHCPClient>();

        public static bool ReleaseClient(DHCPPacketView packet, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            lock(KnownClients)
            {
                var count = KnownClients
                    .RemoveAll(x =>
                        x.ClientId.Equals(packet.ClientId)
                    );

                if (count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Received DHCP release for client ID " + packet.ClientId.ToString() + " but could not find a corresponding lease");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("Received DHCP release for client ID " + packet.ClientId.ToString() + " and processed successfully");
                return true;
            }
        }

        public static DHCPClient FindKnownClient(DHCPPacketView request, IPEndPoint localEndPoint, IPEndPoint remoteEndPoint)
        {
            DHCPClient result = null;
            lock (KnownClients)
                result = KnownClients.Where(x => x.ClientId.Equals(request.ClientId)).FirstOrDefault();

            return result;
        }

        public static void ResetLeaseTimers(DHCPClient client)
        {
            lock (KnownClients)
            {
                var now = DateTimeOffset.Now;

                client.DHCPLease.Requested = now;
                client.DHCPLease.TimesOut = now.Add(client.DHCPLease.Pool.RequestTimeOut);
                client.DHCPLease.Expires = now.Add(client.DHCPLease.Pool.LeaseDuration);
            }
        }

        public static void SetLeaseTimers(DHCPClient client)
        {
            lock (KnownClients)
            {
                var now = DateTimeOffset.Now;

                client.DHCPLease.TimesOut = DateTimeOffset.MaxValue;
                client.DHCPLease.Expires = now.Add(client.DHCPLease.Pool.LeaseDuration);
                if (client.DHCPLease.Acknowledged > DateTimeOffset.MinValue)
                    client.DHCPLease.Renewed = now;
                else
                    client.DHCPLease.Acknowledged = now;
            }
        }

        public static void Add(DHCPClient client)
        {
            lock (KnownClients) KnownClients.Add(client);
        }
    }
}
