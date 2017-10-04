using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NetPlugAndPlay.Models
{
    public class SampleData
    {
        public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
        {
            
            using (var serviceScope = serviceProvider.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var db = serviceScope.ServiceProvider.GetService<PnPServerContext>();

                if (await db.Database.EnsureCreatedAsync())
                {
                    var newNetworkDeviceType = new NetworkDeviceType
                    {
                        Name = "WS-C2960G-8TC-L",
                        Manufacturer = "Cisco Systems",
                        ProductId = "WS-C2960G-8TC-L",
                        Interfaces = new List<NetworkInterface>
                        {
                            new NetworkInterface { Name = "FastEthernet0/1", InterfaceIndex = 1 },
                            new NetworkInterface { Name = "FastEthernet0/2", InterfaceIndex = 2 },
                            new NetworkInterface { Name = "FastEthernet0/3", InterfaceIndex = 3 },
                            new NetworkInterface { Name = "FastEthernet0/4", InterfaceIndex = 4 },
                            new NetworkInterface { Name = "FastEthernet0/5", InterfaceIndex = 5 },
                            new NetworkInterface { Name = "FastEthernet0/6", InterfaceIndex = 6 },
                            new NetworkInterface { Name = "FastEthernet0/7", InterfaceIndex = 7 },
                            new NetworkInterface { Name = "FastEthernet0/8", InterfaceIndex = 8 },
                            new NetworkInterface { Name = "GigabitEthernet0/1", InterfaceIndex = 9 },
                            new NetworkInterface { Name = "GigabitEthernet0/2", InterfaceIndex = 10 },
                        }
                    };

                    //await InsertTestData(serviceProvider);
                    await db.NetworkDeviceTypes.AddAsync(newNetworkDeviceType);

                    var newNetworkDevices = new List<NetworkDevice> {
                            new NetworkDevice
                            {
                                Hostname = "bob",
                                DomainName = "minions.com",
                                Description = "Minions need love too",
                                DeviceType = newNetworkDeviceType,
                                IPAddress = "10.100.5.3"
                            },
                            new NetworkDevice
                            {
                                Hostname = "kevin",
                                DomainName = "minions.com",
                                Description = "Banana!!!",
                                DeviceType = newNetworkDeviceType,
                                IPAddress = "10.0.0.1"
                            }
                        };

                    await db.NetworkDevices.AddRangeAsync(newNetworkDevices);

                    await db.SaveChangesAsync();
                }
            }
        }

    }
}
