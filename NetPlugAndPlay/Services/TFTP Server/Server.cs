using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Tftp.Net;
using System.Text.RegularExpressions;
using libterminal.Helpers.Parsers;
using libterminal.Helpers.Model;
using System.Collections.Generic;

namespace NetPlugAndPlay.Services.TFTP_Server
{
    public class Server
    {
        private static Server s_instance = null;
        TftpServer m_server;

        public Server()
        {
            if(s_instance != null)
            {
                throw new Exception("Only a single instance of TFTP Server can be instantiated at a time");
            }
            s_instance = this;

            m_server = new TftpServer();
            m_server.OnReadRequest += server_OnReadRequest;
            m_server.OnWriteRequest += server_OnWriteRequest;
            m_server.Start();
        }

        private static Regex matchFileNames = new Regex(@"^\s*(cisconet\.cfg|[A-Za-z]+-confg|config\.text|config.txt)\s*$");

        private string GetPhysicalInterfaceName(string interfaceName)
        {
            // TODO : Switch to using a parser for this

            var parts = interfaceName.Split('.');
            if (parts == null || parts.Count() == 1)
                return interfaceName;

            return parts[0];
        }

        private NetworkDevice MatchCDPEntry(PnPServerContext dbContext, ShowCDPEntryItem cdpEntry)
        {
            var deviceId = cdpEntry.DeviceID.ToLowerInvariant();

            var neighborDevice = dbContext.NetworkDevices
                .Where(x =>
                    x.Hostname.ToLower() == cdpEntry.DeviceID ||
                    (x.Hostname + "." + x.DomainName).ToLowerInvariant() == deviceId
                )
                .Include("DeviceType")
                .Include("DeviceType.Interfaces")
                .FirstOrDefault();

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

            var uplink = dbContext.NetworkDeviceLinks
                .Where(x =>
                    x.ConnectedToDevice.Id == neighborDevice.Id &&
                    x.ConnectedToInterfaceIndex == remoteInterface.InterfaceIndex
                )
                .Include("NetworkDevice")
                .FirstOrDefault();

            if (uplink == null)
                return null;

            return uplink.NetworkDevice;
        }

        void TransferConfigurationToDevice(IPAddress address)
        {
            var uri = new Uri("telnet://initialConfig:Minions12345@" + address.ToString());
            var copyResult = libterminal.Helpers.TaskCopy.Run(
                uri,
                "Minions12345",
                "tftp://10.100.11.55/config.txt",
                "foo1.txt"
            );
            System.Diagnostics.Debug.WriteLine("Copied config");
        }

        private class RegisteredDevice
        {
            public IPAddress HostAddress { get; set; }
            public Guid NetworkDeviceId { get; set; }
            public DateTimeOffset TimeExpires { get; set; }
        }

        private List<RegisteredDevice> RegisteredDevices = new List<RegisteredDevice>();

        private void RegisterDeviceMatch(IPAddress deviceAddress, NetworkDevice networkDevice)
        {
            lock(RegisteredDevices)
            {
                // TODO : make the time span an option
                var now = DateTimeOffset.Now;
                RegisteredDevices.RemoveAll(x => x.TimeExpires <= now);
                RegisteredDevices.RemoveAll(x => x.HostAddress == deviceAddress);
                RegisteredDevices.Add(new RegisteredDevice
                {
                    HostAddress = deviceAddress,
                    NetworkDeviceId = networkDevice.Id,
                    TimeExpires = now.Add(TimeSpan.FromSeconds(300))
                });
            }
        }

        private Guid FindRegisteredDevice(EndPoint endPoint)
        {
            if (endPoint is IPEndPoint)
                return FindRegisteredDevice((endPoint as IPEndPoint).Address);

            return Guid.Empty;
        }

        private Guid FindRegisteredDevice(IPAddress deviceAddress)
        {
            lock(RegisteredDevices)
            {
                var now = DateTimeOffset.Now;
                RegisteredDevices.RemoveAll(x => x.TimeExpires <= now);

                System.Diagnostics.Debug.WriteLine("Finding registered device " + deviceAddress.ToString());
                var match = RegisteredDevices
                    .Where(x =>
                        x.HostAddress.Equals(deviceAddress)
                    )
                    .FirstOrDefault();

                if (match == null)
                {
                    System.Diagnostics.Debug.WriteLine("Didn't find registered device " + deviceAddress.ToString());
                    return Guid.Empty;
                }
                System.Diagnostics.Debug.WriteLine("Found registered device " + deviceAddress.ToString() + " as " + match.NetworkDeviceId.ToString());

                match.TimeExpires = now.Add(TimeSpan.FromSeconds(300));

                return match.NetworkDeviceId;
            }
        }

        private void OnReadRequest(
            ITftpTransfer transfer, 
            EndPoint client
            )
        {
            System.Diagnostics.Debug.WriteLine("TFTP request from " + client.ToString() + " for file " + transfer.Filename);
            string connectionString = string.Empty;

            try
            {
                // TODO : Make sure that this code doesn't run until the startup is complete
                connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
            }
            catch
            {
                transfer.Cancel(TftpErrorPacket.FileNotFound);
                return;
            }

            var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
            dbOptions.UseSqlServer(connectionString);

            var dbContext = new PnPServerContext(dbOptions.Options);

            string ipAddress = "";
            if(client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                ipAddress = (client as IPEndPoint).Address.ToString();
            } else if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
            {
                ipAddress = (client as IPEndPoint).Address.ToString();
            }

            NetworkDevice networkDevice = null;
            var networkDeviceId = FindRegisteredDevice(client);
            if (networkDeviceId == Guid.Empty)
                networkDevice = dbContext.NetworkDevices.Where(x => x.IPAddress == ipAddress).FirstOrDefault();
            else
                networkDevice = dbContext.NetworkDevices.Where(x => x.Id == networkDeviceId).FirstOrDefault();

            if(networkDevice == null)
            {
                var m = matchFileNames.Match(transfer.Filename);
                if (m.Success)
                {
                    System.Diagnostics.Debug.WriteLine("Client " + client.ToString() + " requesting " + transfer.Filename + " but the device is not known yet");
                    transfer.Cancel(TftpErrorPacket.FileNotFound);

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
                        }
                        if(entries != null)
                        {
                            System.Diagnostics.Debug.WriteLine("Found " + entries.Count.ToString() + " when querying device " + client.ToString());
                            foreach(var entry in entries)
                            {
                                var cdpMatch = MatchCDPEntry(dbContext, entry);
                                if(cdpMatch != null)
                                {
                                    System.Diagnostics.Debug.WriteLine("Device " + hostName + " identified as " + cdpMatch.Hostname + "." + cdpMatch.DomainName);
                                    RegisterDeviceMatch((client as IPEndPoint).Address, cdpMatch);
                                    TransferConfigurationToDevice((client as IPEndPoint).Address);
                                    break;
                                }
                            }
                        }
                    }
                }
                else
                {
                    var config = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(ipAddress, transfer.Filename, dbContext); }).Result;
                    if (string.IsNullOrEmpty(config))
                    {
                        transfer.Cancel(TftpErrorPacket.FileNotFound);
                        return;
                    }
                    else
                    {
                        var data = new MemoryStream(Encoding.ASCII.GetBytes(config));
                        transfer.Start(data);
                    }
                }
            }
            else
            {
                var m = matchFileNames.Match(transfer.Filename);
                if(m.Success)
                {
                    var config = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(networkDevice.Id, dbContext); }).Result;
                    var data = new MemoryStream(Encoding.ASCII.GetBytes(config));
                    transfer.Start(data);
                }
                else
                {
                    transfer.Cancel(TftpErrorPacket.FileNotFound);
                }
            }
        }

        static void server_OnReadRequest(ITftpTransfer transfer, EndPoint client)
        {
            if(s_instance == null)
            {
                throw new Exception("Invalid state, no static instance of TFTP Server");
            }
            s_instance.OnReadRequest(transfer, client);
        }

        static void server_OnWriteRequest(ITftpTransfer transfer, EndPoint client)
        {
            // TODO : Consider supporting uploading of files. This can be used for GIT integration
        }
    }
}
