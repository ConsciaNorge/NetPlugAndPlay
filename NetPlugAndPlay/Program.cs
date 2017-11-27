using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace NetPlugAndPlay
{
    public class Program
    {
        static Services.TFTP_Server.Server tftpServer;
        static Services.DHCP_Server.Server dhcpServer;
        static Services.CDPWalker.CDPWalker walker;

        public static void Main(string[] args)
        {
            tftpServer = new Services.TFTP_Server.Server();
            dhcpServer = new Services.DHCP_Server.Server();
            walker = new Services.CDPWalker.CDPWalker();
            //walker.AddClient(System.Net.IPAddress.Parse("10.100.5.3"));

            BuildWebHost(args).Run();
        }

        public static IWebHost BuildWebHost(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .Build();
    }
}
