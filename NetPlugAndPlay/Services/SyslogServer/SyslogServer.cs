/// The MIT License(MIT)
/// 
/// Copyright(c) 2017 Conscia Norway AS
/// 
/// Permission is hereby granted, free of charge, to any person obtaining a copy
/// of this software and associated documentation files (the "Software"), to deal
/// in the Software without restriction, including without limitation the rights
/// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
/// copies of the Software, and to permit persons to whom the Software is
/// furnished to do so, subject to the following conditions:
/// 
/// The above copyright notice and this permission notice shall be included in all
/// copies or substantial portions of the Software.
/// 
/// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
/// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
/// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
/// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
/// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
/// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
/// SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Services.SyslogServer
{
    public class SyslogServer
    {
        private const int MAX_BUFFER_SIZE = 16384;

        IPEndPoint listenerEndpoint;
        UdpClient listener;

        public Func<object, SyslogMessageEventArgs, Task> OnSyslogMessage;
        //public EventHandler<SyslogMessageEventArgs> OnSyslogMessage;

        private static SyslogServer Instance { get; set; }

        public SyslogServer()
        {
            if (Instance != null)
                throw new Exception("There can only be one syslog server instance running at a time");

            Instance = this;
            Task.Factory.StartNew(async () => { await Instance.Start(); });
        }

        public async Task<bool> Start()
        {
            try
            {
                listenerEndpoint = new IPEndPoint(IPAddress.Any, 514);
                listener = new UdpClient(listenerEndpoint);

                while (true)
                {
                    // TODO : Add task for cancel event
                    var readTask = listener.ReceiveAsync();
                    var completedTask = await Task.WhenAny(readTask).ConfigureAwait(false);

                    if (completedTask == readTask)
                    {
                        await ProcessReceivedPacket(readTask.Result.Buffer, readTask.Result.RemoteEndPoint);
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
                return false;
            }
        }

        public async Task<bool> ProcessReceivedPacket(byte[] buffer, IPEndPoint remoteEndPoint)
        {
            var message = Encoding.ASCII.GetString(buffer, 0, buffer.Length);
            System.Diagnostics.Debug.WriteLine("Buffer received from " + remoteEndPoint.ToString() + " - " + message);

            if (OnSyslogMessage == null)
                return false;

            Delegate[] invocationList = OnSyslogMessage.GetInvocationList();
            Task[] onSyslogMessageTasks = new Task[invocationList.Length];

            for (int i = 0; i < invocationList.Length; i++)
                onSyslogMessageTasks[i] = ((Func<object, SyslogMessageEventArgs, Task>)invocationList[i]) (this, new SyslogMessageEventArgs { Host = remoteEndPoint, Message = message });

            await Task.WhenAll(onSyslogMessageTasks);

            return true;
        }

        private Dictionary<IPAddress, IPAddress> RouteCache = new Dictionary<IPAddress, IPAddress>();

        private IPAddress QueryRoutingInterface(
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

        private IPEndPoint QueryRoutingInterface(
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
