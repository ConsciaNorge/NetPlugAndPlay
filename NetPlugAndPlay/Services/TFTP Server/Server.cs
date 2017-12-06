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
            var networkDeviceId = RegisteredDevices.FindRegisteredDevice(client);
            if (networkDeviceId == Guid.Empty)
                networkDevice = dbContext.NetworkDevices.Where(x => x.IPAddress == ipAddress).FirstOrDefault();
            else
                networkDevice = dbContext.NetworkDevices.Where(x => x.Id == networkDeviceId).FirstOrDefault();

            if (networkDevice != null)
            {
                var m = matchFileNames.Match(transfer.Filename);
                if (m.Success)
                {
                    var configText = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(networkDevice.Id, dbContext); }).Result;
                    var data = new MemoryStream(Encoding.ASCII.GetBytes(configText));
                    transfer.Start(data);
                    return;
                }
            }

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
