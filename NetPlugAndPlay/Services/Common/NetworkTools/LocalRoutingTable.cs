using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.Common.NetworkTools
{
    public class LocalRoutingTable
    {
        static private Dictionary<IPAddress, IPAddress> RouteCache = new Dictionary<IPAddress, IPAddress>();

        static public IPAddress QueryRoutingInterface(
            IPAddress remoteIp)
        {
            IPAddress result;
            if (!RouteCache.TryGetValue(remoteIp, out result))
            {
                var remoteEndPoint = new IPEndPoint(remoteIp, 0);
                var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                result = QueryRoutingInterface(socket, remoteEndPoint).Address;
                RouteCache.Add(remoteIp, result);
            }
            return result;
        }

        static private IPEndPoint QueryRoutingInterface(
                  Socket socket,
                  IPEndPoint remoteEndPoint)
        {
            var socketAddress = remoteEndPoint.Serialize();

            var remoteAddressBytes = new byte[socketAddress.Size];
            for (int i = 0; i < socketAddress.Size; i++)
                remoteAddressBytes[i] = socketAddress[i];

            var outBytes = new byte[remoteAddressBytes.Length];
            socket.IOControl(IOControlCode.RoutingInterfaceQuery, remoteAddressBytes, outBytes);

            for (int i = 0; i < socketAddress.Size; i++)
                socketAddress[i] = outBytes[i];

            return (IPEndPoint)remoteEndPoint.Create(socketAddress);
        }
    }
}
