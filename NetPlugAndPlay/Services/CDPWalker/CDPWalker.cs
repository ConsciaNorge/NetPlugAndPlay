using libterminal.Helpers;
using libterminal.Helpers.Model;
using libterminal.Helpers.Parsers;
using NetPlugAndPlay.Services.CDPWalker.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Timers;

namespace NetPlugAndPlay.Services.CDPWalker
{
    public class CDPWalker
    {
        static CDPWalker Instance { get; set; } = null;

        public List<CDPClient> Clients { get; set; } = new List<CDPClient>();

        Timer timer = null;

        public CDPWalker()
        {
            if (Instance != null)
                throw new Exception("CDPWalker can only be instantiated once");
            Instance = this;

            timer = new Timer(3000);
            timer.Elapsed += async (sender, e) => await scannerTimerCallback();
            timer.Start();
        }

        private Task scannerTimerCallback()
        {
            timer.Stop();

            var now = DateTimeOffset.Now;

            List<CDPClient> expiredClients;

            // TODO : Lock clients more intelligently.
            lock (Clients)
                expiredClients = Clients.Where(x => now.Subtract(x.TimeLastObserved).TotalSeconds > 30).ToList();

            foreach (var client in expiredClients)
            {
                var uri = new Uri("telnet://priv0user:Minions54321@" + client.ManagementIP.ToString());

                var entriesText = libterminal.Helpers.TaskShowCDPEntries.Run(
                    uri,
                    "Minions12345"
                );

                if (string.IsNullOrWhiteSpace(entriesText))
                    System.Diagnostics.Debug.WriteLine("Received null or whitespace result to 'show cdp entries *'");

                client.ShowCDPText = entriesText;
                if (client.TimeOriginallyObserved == DateTimeOffset.FromUnixTimeSeconds(0))
                    client.TimeOriginallyObserved = now;
                client.TimeLastObserved = now;

                foreach(var neighbor in client.CDPEntries)
                {
                    foreach (var entryAddress in neighbor.EntryAddresses)
                    {
                        if(entryAddress.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            AddClient(entryAddress);
                    }
                }
            }
            timer.Start();

            // TODO : Come up with a far less ugly solution that this
            return Task.Delay(10);
        }

        public CDPClient AddClient(IPAddress managementIP)
        {
            return AddClient(managementIP, Guid.Empty);
        }

        public CDPClient AddClient(IPAddress managementIP, Guid networkDeviceId)
        {
            lock (Clients)
            {
                var existingClient = Clients.Where(x => x.ManagementIP.Equals(managementIP)).FirstOrDefault();

                if (existingClient != null)
                {
                    System.Diagnostics.Debug.WriteLine("CDP Client " + managementIP.ToString() + " already exists");
                    return existingClient;
                }

                var newClient = new CDPClient
                {
                    ManagementIP = managementIP,
                    NetworkDeviceId = networkDeviceId
                };

                Clients.Add(newClient);

                return newClient;
            }
        }
    }
}
