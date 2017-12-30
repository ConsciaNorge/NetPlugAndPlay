using libnetworkutility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetPlugAndPlay.Models;
using NetPlugAndPlay.Services.DeviceConfigurator;
using Serilog;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Tftp.Net;

namespace NetPlugAndPlay.Services.TFTP_Server
{
    public class Server
    {
        private static Server s_instance = null;
        TftpServer m_server;

        public Server()
        {
            Log.Logger.Here().Information("Starting TFTP Server");
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

        private static Regex RegexConfigText = new Regex(@"^\s*(cisconet\.cfg|[A-Za-z]+-confg|config\.text|config.txt)\s*$", RegexOptions.Compiled);

        private void OnReadRequest(
            ITftpTransfer transfer, 
            EndPoint client
            )
        {
            Log.Logger.Here().Information("Incoming TFTP request from " + client.ToString() + " for file " + transfer.Filename);
            string connectionString = string.Empty;

            string ipAddress = "";
            if(client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                ipAddress = (client as IPEndPoint).Address.ToString();
            else if (client.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
                ipAddress = (client as IPEndPoint).Address.ToString();

            var m = RegexConfigText.Match(transfer.Filename);
            if (m.Success)
            {
                try
                {
                    // TODO : Make sure that this code doesn't run until the startup is complete
                    connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
                }
                catch (Exception e)
                {
                    Log.Logger.Here().Error(e, "Attempted to read configuration from startup before startup is completed due to premature handling of TFTP request.");
                    transfer.Cancel(TftpErrorPacket.FileNotFound);
                    return;
                }

                var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
                dbOptions.UseSqlServer(connectionString);

                var dbContext = new PnPServerContext(dbOptions.Options);

                NetworkDevice networkDevice = null;
                var networkDeviceId = RegisteredDevices.FindRegisteredDevice(client);
                if (networkDeviceId == Guid.Empty)
                    networkDevice = dbContext.NetworkDevices.Where(x => x.IPAddress == ipAddress).FirstOrDefault();
                else
                    networkDevice = dbContext.NetworkDevices.Where(x => x.Id == networkDeviceId).FirstOrDefault();

                // If the device is unknown by cache or IP address, then try matching it to a /30 subnet of the upstream device
                if (networkDevice == null)
                {
                    string slash24 = ipAddress.Substring(0, ipAddress.LastIndexOf('.'));
                    Log.Logger.Here().Debug("Device not found by cache or exact IP match. Attempting to match to a /30 upstream device with the first /24 bits as " + slash24);

                    var possibleDevices = dbContext.NetworkDevices
                        .Where(x =>
                            x.Network.EndsWith("/30") &&
                            x.Network.StartsWith(slash24)
                        )
                        .ToList();

                    if (possibleDevices != null)
                    {
                        Log.Logger.Here().Debug("There are " + possibleDevices.Count.ToString() + " /30 uplink devices found with prefixes matching the first 24 bits " + slash24);
                        var upstreamDevice = possibleDevices
                            .Where(x =>
                                NetworkPrefix.Parse(x.Network).Contains(IPAddress.Parse(ipAddress))
                            )
                            .FirstOrDefault();

                        if (upstreamDevice != null)
                        {
                            Log.Logger.Here().Debug("Found exact match for " + ipAddress + " as being the only device connected to a /30 network on " + upstreamDevice.Hostname + "." + upstreamDevice.DomainName);

                            var connectedLink = dbContext.NetworkDeviceLinks
                                .Where(x =>
                                    x.ConnectedToDevice.Id == upstreamDevice.Id
                                )
                                .Include("NetworkDevice")
                                .FirstOrDefault();

                            // TODO : Refactor configuration system for networks on links. This is too single-dimensional
                            if (connectedLink != null)
                            {
                                networkDevice = connectedLink.NetworkDevice;
                                Log.Logger.Here().Debug("Found a device connected to the upstream /30 device " + upstreamDevice.IPAddress + " assuming this device is the correct one " + networkDevice.Hostname + "." + networkDevice.DomainName);
                            }
                            else
                                Log.Logger.Here().Debug("No devices seem to be configured as connected to " + upstreamDevice.Network);
                        }
                        else
                            Log.Logger.Here().Debug("Could not find any /30 upstream network devices for " + ipAddress);
                    }
                    else
                        Log.Logger.Here().Debug("Could not find any /30 upstream network devices for " + ipAddress);
                }

                if (networkDevice == null)
                {
                    Log.Logger.Here().Warning("Request for " + transfer.Filename + " from " + ipAddress.ToString() + " unhandled as the network device is not known");
                }
                else
                {
                    Log.Logger.Here().Debug("Generating configuration for " + ipAddress.ToString() + " which is identified as " + networkDevice.Hostname + "." + networkDevice.DomainName);
                    var configText = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(networkDevice.Id, dbContext); }).Result;

                    if (string.IsNullOrEmpty(configText))
                    {
                        Log.Logger.Here().Warning("Failed to generate configuration for " + ipAddress.ToString() + " which is identified as " + networkDevice.Hostname + "." + networkDevice.DomainName);
                        transfer.Cancel(TftpErrorPacket.FileNotFound);
                        return;
                    }
                    else
                    {
                        var data = new MemoryStream(Encoding.ASCII.GetBytes(configText));
                        Log.Logger.Here().Debug("Transmitting via TFTP " + data.Length.ToString() + " bytes of configuration for request from " + ipAddress.ToString() + " for " + transfer.Filename);

                        transfer.Start(data);
                        return;
                    }
                }
            }
            else
            {
                try
                {
                    // TODO : Make sure that this code doesn't run until the startup is complete
                    connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
                }
                catch (Exception e)
                {
                    Log.Logger.Here().Error(e, "Attempted to read configuration from startup before startup is completed due to premature handling of TFTP request.");
                    transfer.Cancel(TftpErrorPacket.FileNotFound);
                    return;
                }

                var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
                dbOptions.UseSqlServer(connectionString);

                var dbContext = new PnPServerContext(dbOptions.Options);

                Log.Logger.Here().Debug("Generating configuration for " + transfer.Filename + " requested by " + ipAddress.ToString());
                var config = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(ipAddress, transfer.Filename, dbContext); }).Result;
                if (string.IsNullOrEmpty(config))
                {
                    Log.Logger.Here().Warning("Failed to generate configuration for " + transfer.Filename + " requested by " + ipAddress.ToString() + " because it either didn't exist or was not a valid format");
                    transfer.Cancel(TftpErrorPacket.FileNotFound);
                    return;
                }
                else
                {
                    var data = new MemoryStream(Encoding.ASCII.GetBytes(config));
                    Log.Logger.Here().Debug("Transmitting via TFTP " + data.Length.ToString() + " bytes of configuration for request from " + ipAddress.ToString() + " for " + transfer.Filename);

                    transfer.Start(data);
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
