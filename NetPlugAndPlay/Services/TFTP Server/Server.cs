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
using NetPlugAndPlay.Services.DeviceConfigurator;
using Serilog;

namespace NetPlugAndPlay.Services.TFTP_Server
{
    public class Server
    {
        private static Server s_instance = null;
        TftpServer m_server;

        public Server()
        {
            Log.Information("Starting TFTP Server");
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
            Log.Information("Incoming TFTP request from " + client.ToString() + " for file " + transfer.Filename);
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
                    Log.Error(e, "Attempted to read configuration from startup before startup is completed due to premature handling of TFTP request.");
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

                if (networkDevice == null)
                {
                    Log.Warning("Request for " + transfer.Filename + " from " + ipAddress.ToString() + " unhandled as the network device is not known");
                }
                else
                {
                    Log.Debug("Generating configuration for " + ipAddress.ToString() + " which is identified as " + networkDevice.Hostname + "." + networkDevice.DomainName);
                    var configText = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(networkDevice.Id, dbContext); }).Result;

                    if (string.IsNullOrEmpty(configText))
                    {
                        Log.Warning("Failed to generate configuration for " + ipAddress.ToString() + " which is identified as " + networkDevice.Hostname + "." + networkDevice.DomainName);
                        transfer.Cancel(TftpErrorPacket.FileNotFound);
                        return;
                    }
                    else
                    {
                        var data = new MemoryStream(Encoding.ASCII.GetBytes(configText));
                        Log.Debug("Transmitting via TFTP " + data.Length.ToString() + " bytes of configuration for request from " + ipAddress.ToString() + " for " + transfer.Filename);

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
                    Log.Error(e, "Attempted to read configuration from startup before startup is completed due to premature handling of TFTP request.");
                    transfer.Cancel(TftpErrorPacket.FileNotFound);
                    return;
                }

                var dbOptions = new DbContextOptionsBuilder<PnPServerContext>();
                dbOptions.UseSqlServer(connectionString);

                var dbContext = new PnPServerContext(dbOptions.Options);

                Log.Debug("Generating configuration for " + transfer.Filename + " requested by " + ipAddress.ToString());
                var config = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(ipAddress, transfer.Filename, dbContext); }).Result;
                if (string.IsNullOrEmpty(config))
                {
                    Log.Warning("Failed to generate configuration for " + transfer.Filename + " requested by " + ipAddress.ToString() + " because it either didn't exist or was not a valid format");
                    transfer.Cancel(TftpErrorPacket.FileNotFound);
                    return;
                }
                else
                {
                    var data = new MemoryStream(Encoding.ASCII.GetBytes(config));
                    Log.Debug("Transmitting via TFTP " + data.Length.ToString() + " bytes of configuration for request from " + ipAddress.ToString() + " for " + transfer.Filename);

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
