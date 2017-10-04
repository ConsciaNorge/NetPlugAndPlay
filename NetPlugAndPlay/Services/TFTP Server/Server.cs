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

        private void OnReadRequest(
            ITftpTransfer transfer, 
            EndPoint client
            )
        {
            var connectionString = Startup.Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString");
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
            var networkDevice = dbContext.NetworkDevices.Where(x => x.IPAddress == ipAddress).FirstOrDefault();

            if(networkDevice == null)
            {
                transfer.Cancel(TftpErrorPacket.FileNotFound);
            }
            else
            {
                if(transfer.Filename.ToLowerInvariant().Equals("config.txt"))
                {
                    var config = Task.Run<string>(() => { return ConfigurationGenerator.Generator.Generate(ipAddress, dbContext); }).Result;
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
