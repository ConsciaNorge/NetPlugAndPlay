using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NetPlugAndPlay.Models;
using Serilog;
using NetPlugAndPlay.Controllers.v0.ViewModels;
using Microsoft.EntityFrameworkCore;
using NetPlugAndPlay.PlugAndPlayTools.Cisco;
using LibDHCPServer.VolatilePool;

namespace NetPlugAndPlay.Controllers.v0
{
    [Produces("application/json")]
    [Route("api/v0/batch")]
    public class BatchController : Controller
    {
        // PUT api/values/5
        [HttpPost()]
        public async Task<IActionResult> Put(
                [FromServices] PnPServerContext dbContext,
                Guid id,
                [FromBody] BatchPutViewModel changes
            )
        {
            Log.Logger.Here().Debug("PUT " + Url.ToString() + " called from " + HttpContext.Connection.RemoteIpAddress.ToString());

            var result = new List<string>();

            try
            {
                Log.Logger.Here().Debug(" Processing removals from incoming changes");
                result.AddRange(await ProcessRemovals(dbContext, changes));
            }
            catch (Exception e)
            {
                Log.Logger.Here().Debug(e, "Failed to queue removal of objects per request");
                return BadRequest();
            }

            try
            {
                Log.Logger.Here().Debug(" Processing changes from incoming changes");
                result.AddRange(ProcessChanges(dbContext, changes));
            }
            catch (Exception e)
            {
                Log.Logger.Here().Debug(e, "Failed to queue changes of objects per request");
                return BadRequest();
            }

            try
            {
                Log.Logger.Here().Debug(" Processing additions from incoming changes");
                result.AddRange(ProcessAdditions(dbContext, changes));
            }
            catch (Exception e)
            {
                Log.Logger.Here().Debug(e, "Failed to queue additions of objects per request");
                return BadRequest();
            }

            Log.Logger.Here().Debug("Applying changes");
            await dbContext.SaveChangesAsync();
            result.Add("Applying changes to the database");

            try
            {
                Log.Logger.Here().Debug(" Processing connectionsfrom incoming changes");
                result.AddRange(ProcessConnections(dbContext, changes));
            }
            catch (Exception e)
            {
                Log.Logger.Here().Debug(e, "Failed to queue connections of objects per request");
                return BadRequest();
            }

            Log.Logger.Here().Debug("Applying changes");
            await dbContext.SaveChangesAsync();
            result.Add("Applying changes to the database");

            return new ObjectResult(new BatchPutResultViewModel
            {
                Changes = result
            });
        }

        private IEnumerable<string> ProcessConnections(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            Log.Logger.Here().Debug("  Processing connection removals");
            result.AddRange(ProcessConnectionRemovals(dbContext, changes));

            Log.Logger.Here().Debug("  Processing connection additions");
            result.AddRange(ProcessConnectionAdditions(dbContext, changes));

            return result;
        }

        private List<string> ProcessConnectionAdditions(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.Connections == null || changes.Connections.ToAdd == null || changes.Connections.ToAdd.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no network connections specified to be added");
                return result;
            }

            Log.Logger.Here().Debug("    Creating the network connections objects specified to be added");
            //var connectionsToAdd = 
            //    (
            //        from connection in changes.Connections.ToAdd
            //        join device in dbContext.NetworkDevices
            //        on (connection.NetworkDevice + '.' + connection.DomainName) equals (device.Hostname + '.' + device.DomainName)
            //        join uplinkDevice in dbContext.NetworkDevices
            //        on (connection.UplinkToDevice + '.' + connection.DomainName) equals (uplinkDevice.Hostname + '.' + uplinkDevice.DomainName)
            //        select (
            //            new NetworkDeviceLink
            //            {
            //                NetworkDevice = device,
            //                ConnectedToDevice = uplinkDevice,
            //                InterfaceIndex =
            //                    device.DeviceType.Interfaces
            //                        .Where(x =>
            //                            x.Name == connection.Interface
            //                         )
            //                         .Select(x =>
            //                            x.InterfaceIndex
            //                         )
            //                         .FirstOrDefault(),
            //                ConnectedToInterfaceIndex =
            //                    uplinkDevice.DeviceType.Interfaces
            //                        .Where(x =>
            //                            x.Name == connection.UplinkToInterface
            //                        )
            //                        .Select(x =>
            //                            x.InterfaceIndex
            //                        )
            //                        .FirstOrDefault()
            //            }
            //        )
            //    )
            //    .ToList();

            var connectionsToAdd = new List<NetworkDeviceLink>();
            foreach(var item in changes.Connections.ToAdd)
            {
                var networkDevice = dbContext.NetworkDevices
                    .Where(x =>
                        x.Hostname == item.NetworkDevice &&
                        x.DomainName == item.DomainName
                    )
                    .Include("DeviceType.Interfaces")
                    .FirstOrDefault();

                if (networkDevice == null)
                    throw new Exception("Can't find network device " + item.NetworkDevice + "." + item.DomainName + " when trying to add a connection");

                var networkInterface = networkDevice.DeviceType.Interfaces
                    .Where(x =>
                        x.Name == item.Interface
                    )
                    .FirstOrDefault();

                if (networkInterface == null)
                    throw new Exception("Can't find network interface " + item.Interface + " on network device " + item.NetworkDevice + "." + item.DomainName);

                var uplinkDevice = dbContext.NetworkDevices
                    .Where(x =>
                        x.Hostname == item.UplinkToDevice &&
                        x.DomainName == item.DomainName
                    )
                    .Include("DeviceType.Interfaces")
                    .FirstOrDefault();

                if (uplinkDevice == null)
                    throw new Exception("Can't find network device " + item.NetworkDevice + "." + item.DomainName + " when trying to add a connection");

                var uplinkInterface = uplinkDevice.DeviceType.Interfaces
                    .Where(x =>
                        x.Name == item.UplinkToInterface
                    )
                    .FirstOrDefault();

                if (uplinkInterface == null)
                    throw new Exception("Can't find network interface " + item.UplinkToInterface + " on network device " + item.UplinkToDevice + "." + item.DomainName);

                connectionsToAdd.Add(new NetworkDeviceLink
                {
                    NetworkDevice = networkDevice,
                    InterfaceIndex = networkInterface.InterfaceIndex,
                    ConnectedToDevice = uplinkDevice,
                    ConnectedToInterfaceIndex = uplinkInterface.InterfaceIndex
                });
            }

            if(connectionsToAdd.Count > 0)
            {
                Log.Logger.Here().Debug("    Queuing " + connectionsToAdd.Count.ToString() + " connections to be added");
                dbContext.NetworkDeviceLinks.AddRange(connectionsToAdd);
                result.Add("    Queued " + connectionsToAdd.Count.ToString() + " connections to be added");
            }

            return result;
        }


        private List<string> ProcessChanges(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            Log.Logger.Here().Debug("  Processing changes of templates");
            result.AddRange(ProcessTemplateChanges(dbContext, changes));

            Log.Logger.Here().Debug("  Processing changes of tftp files");
            result.AddRange(ProcessTFTPFileChanges(dbContext, changes));

            Log.Logger.Here().Debug("  Processing changes of network device types");
            result.AddRange(ProcessNetworkDeviceTypeChanges(dbContext, changes));

            Log.Logger.Here().Debug("  Processing changes of network devices");
            result.AddRange(ProcessNetworkDeviceChanges(dbContext, changes));

            return result;
        }

        private IEnumerable<string> ProcessNetworkDeviceChanges(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.NetworkDevices == null || changes.NetworkDevices.ToChange == null || changes.NetworkDevices.ToChange.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no network devices specified to be changed");
                return result;
            }

            foreach (var device in changes.NetworkDevices.ToChange)
            {
                Log.Logger.Here().Debug("    Preparing device " + device.Id.ToString() + " to change");

                var existingDevice = dbContext.NetworkDevices
                    .Where(x =>
                        x.Id == device.Id
                    )
                    .Include("DHCPExclusions")
                    .FirstOrDefault();

                if (existingDevice == null)
                    throw new Exception("Failed to find network device with id " + device.Id.ToString());

                Log.Logger.Here().Debug("     Changing network device field values for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                if (device.DeviceType != null)
                {
                    var deviceType = dbContext.NetworkDeviceTypes
                        .Where(x => x.Name == device.DeviceType)
                        .FirstOrDefault();

                    if (deviceType == null)
                        throw new Exception("Failed to find the device type " + device.DeviceType + " for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                    existingDevice.DeviceType = deviceType;
                }
                if (device.Hostname != null) { existingDevice.Hostname = device.Hostname; }
                if (device.DomainName != null) { existingDevice.DomainName = device.DomainName; }
                if (device.Description != null) { existingDevice.Description = device.Description; }
                if (device.IPAddress != null)
                {
                    var prefix = NetworkPrefix.Parse(device.IPAddress);
                    existingDevice.IPAddress = prefix.Network.ToString();
                    existingDevice.Network = prefix.BaseNetwork.ToString();
                }
                if (device.DHCPRelay.HasValue) { existingDevice.DHCPRelay = device.DHCPRelay.Value; }
                if (device.DHCPTftpBootfile != null) { existingDevice.DHCPTftpBootfile = device.DHCPTftpBootfile; }

                if (device.DHCPExclusions != null)
                {
                    Log.Logger.Here().Debug("     Changing DHCP exclusion values for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                    if (device.DHCPExclusions.ToRemove != null && device.DHCPExclusions.ToRemove.Count > 0)
                    {
                        Log.Logger.Here().Debug("      Queuing removal of DHCP exclusions for" + existingDevice.Hostname + "." + existingDevice.DomainName);

                        var toRemove = existingDevice.DHCPExclusions
                            .Where(x =>
                                device.DHCPExclusions.ToRemove.Contains(x.Id)
                            )
                            .ToList();

                        if (toRemove == null || toRemove.Count == 0)
                            throw new Exception("Failed to find DHCP exclusions to remove for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                        dbContext.DhcpExclusions.RemoveRange(toRemove);
                        existingDevice.DHCPExclusions.RemoveAll(x => device.DHCPExclusions.ToRemove.Contains(x.Id));

                        result.Add("      Queued removal of DHCP exclusions for" + existingDevice.Hostname + "." + existingDevice.DomainName);
                    }

                    if (device.DHCPExclusions.ToAdd != null && device.DHCPExclusions.ToAdd.Count > 0)
                    {
                        Log.Logger.Here().Debug("      Queuing addition of DHCP exclusions for" + existingDevice.Hostname + "." + existingDevice.DomainName);

                        existingDevice.DHCPExclusions.AddRange(
                            device.DHCPExclusions.ToAdd
                                .Select(x =>
                                    new DHCPExclusion
                                    {
                                        Start = x.Start,
                                        End = x.End
                                    }
                                )
                                .ToList()
                        );

                        result.Add("      Queued addition of DHCP exclusions for" + existingDevice.Hostname + "." + existingDevice.DomainName);
                    }

                    if (device.DHCPExclusions.ToAdd != null && device.DHCPExclusions.ToAdd.Count > 0)
                    {
                        Log.Logger.Here().Debug("      Queuing changes of DHCP exclusions for" + existingDevice.Hostname + "." + existingDevice.DomainName);

                        foreach (var exclusion in device.DHCPExclusions.ToChange)
                        {
                            var toChange = existingDevice.DHCPExclusions
                                .Where(x =>
                                    x.Id == exclusion.Id
                                )
                                .FirstOrDefault();

                            if (toChange == null)
                                throw new Exception("Failed to find DHCP exclusion " + exclusion.Id.ToString() + " to change for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                            if (toChange.Start != null) toChange.Start = exclusion.Start;
                            if (toChange.End != null) toChange.End = exclusion.End;

                            dbContext.DhcpExclusions.Update(toChange);
                        }

                        result.Add("      Queued changes of DHCP exclusions for" + existingDevice.Hostname + "." + existingDevice.DomainName);
                    }
                }

                if(device.Template != null)
                {
                    Log.Logger.Here().Debug("     Changing template configuration values for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                    var templateConfiguration = dbContext.TemplateConfigurations
                        .Where(x =>
                            x.NetworkDevice.Id == existingDevice.Id
                        )
                        .Include("Properties")
                        .FirstOrDefault();

                    if(templateConfiguration == null)
                    {
                        var template = dbContext.Templates
                            .Where(x =>
                                x.Name == device.Template.Name
                            )
                            .FirstOrDefault();

                        if(template == null)
                            throw new Exception("Could not find template " + device.Template.Name + " for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                        Log.Logger.Here().Debug("     Adding template configuration values for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                        templateConfiguration = new TemplateConfiguration
                        {
                            NetworkDevice = existingDevice,
                            Template = template,
                            Description = device.Description,
                            Properties = (device.Template.Parameters == null) ?
                                null :
                                device.Template.Parameters.ToAdd
                                    .Select(x =>
                                        new TemplateProperty
                                        {
                                            Name = x.Name,
                                            Value = x.Value
                                        }
                                    )
                                    .ToList()
                        };
                        result.Add("     Added template configuration values for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                    }
                    else
                    {
                        if(device.Template.Name != null)
                        {
                            var template = dbContext.Templates
                                .Where(x =>
                                    x.Name == device.Template.Name
                                )
                                .FirstOrDefault();

                            if (template == null)
                                throw new Exception("Could not find template " + device.Template.Name + " for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                            templateConfiguration.Template = template;
                        }

                        if(device.Template.Description != null) { templateConfiguration.Description = device.Template.Description; }
                        if(device.Template.Parameters != null)
                        {
                            if (device.Template.Parameters.ToRemove != null && device.Template.Parameters.ToRemove.Count > 0)
                            {
                                Log.Logger.Here().Debug("     Queuing removal of template configuration parameters for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                                var toRemove = templateConfiguration.Properties.Where(x => device.Template.Parameters.ToRemove.Contains(x.Id)).ToList();
                                dbContext.TemplateProperties.RemoveRange(toRemove);
                                templateConfiguration.Properties.RemoveAll(x => device.Template.Parameters.ToRemove.Contains(x.Id));

                                result.Add("     Queued removal of template configuration parameters for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                            }

                            if (device.Template.Parameters.ToAdd != null && device.Template.Parameters.ToAdd.Count > 0)
                            {
                                Log.Logger.Here().Debug("     Queuing addition of template configuration parameters for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                                templateConfiguration.Properties.AddRange(
                                    device.Template.Parameters.ToAdd
                                    .Select(x =>
                                        new TemplateProperty
                                        {
                                            Name = x.Name,
                                            Value = x.Value
                                        }
                                    )
                                    .ToList()
                                );
                                result.Add("     Queued addition of template configuration parameters for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                            }

                            if (device.Template.Parameters.ToChange != null && device.Template.Parameters.ToChange.Count > 0)
                            {
                                Log.Logger.Here().Debug("     Queuing changes of template configuration parameters for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                                foreach(var parameter in device.Template.Parameters.ToChange)
                                {
                                    var toChange = templateConfiguration.Properties
                                        .Where(x =>
                                            x.Id == parameter.Id
                                        )
                                        .FirstOrDefault();

                                    if (toChange == null)
                                        throw new Exception("Could not find template parameter " + parameter.Name + " to change for " + existingDevice.Hostname + "." + existingDevice.DomainName);

                                    if(parameter.Name != null) { toChange.Name = parameter.Name; }
                                    if(parameter.Value != null) { toChange.Value = parameter.Value; }

                                    dbContext.TemplateProperties.Update(toChange);
                                }

                                result.Add("     Queued changes of template configuration parameters for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                            }
                        }
                        dbContext.TemplateConfigurations.Update(templateConfiguration);

                        result.Add("     Qeueued template configuration changes for " + existingDevice.Hostname + "." + existingDevice.DomainName);
                    }

                    dbContext.NetworkDevices.Update(existingDevice);
                }
            }

            return result;
        }

        private List<string> ProcessNetworkDeviceTypeChanges(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.DeviceTypes == null || changes.DeviceTypes.ToChange == null || changes.DeviceTypes.ToChange.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no device types specified to be changed");
                return result;
            }

            foreach(var deviceType in changes.DeviceTypes.ToChange)
            {
                Log.Logger.Here().Debug("    Preparing device type " + deviceType.Name + " to change");

                var existingDeviceType = dbContext.NetworkDeviceTypes
                    .Where(x => 
                        x.Id == deviceType.Id
                    )
                    .Include("Interfaces")
                    .FirstOrDefault();

                if (existingDeviceType == null)
                    throw new Exception("Failed to find network device type with id " + deviceType.Id.ToString());

                Log.Logger.Here().Debug("     Changing network device type field values for " + deviceType.Name);
                if (deviceType.Name != null) existingDeviceType.Name = deviceType.Name;
                if (deviceType.Manufacturer != null) existingDeviceType.Manufacturer = deviceType.Manufacturer;
                if (deviceType.ProductId != null) existingDeviceType.ProductId = deviceType.ProductId;
                if (deviceType.Interfaces != null)
                {
                    if(deviceType.Interfaces.ToRemove != null && deviceType.Interfaces.ToRemove.Count > 0)
                    {
                        Log.Logger.Here().Debug("     Queuing network interface removal for " + deviceType.Name);
                        var toRemove = existingDeviceType.Interfaces
                            .Where(x => 
                                deviceType.Interfaces.ToRemove.Contains(x.Id)
                            )
                            .ToList();

                        if (toRemove != null && toRemove.Count > 0)
                        {
                            dbContext.NetworkInterfaces.RemoveRange(toRemove);
                            existingDeviceType.Interfaces.RemoveAll(x =>
                                deviceType.Interfaces.ToRemove.Contains(x.Id)
                            );
                        }
                    }

                    if(deviceType.Interfaces.ToAdd != null && deviceType.Interfaces.ToAdd.Count > 0)
                    {
                        Log.Logger.Here().Debug("     Queuing network interface additions for " + deviceType.Name);
                        dbContext.NetworkInterfaces.AddRange(
                            deviceType.Interfaces.ToAdd
                                .Select(x =>
                                    new NetworkInterface
                                    {
                                        DeviceType = existingDeviceType,
                                        Name = x.Name,
                                        InterfaceIndex = x.InterfaceIndex.Value
                                    }
                                )
                                .ToList()
                            );
                    }

                    if(deviceType.Interfaces.ToChange != null && deviceType.Interfaces.ToChange.Count > 0)
                    {
                        foreach(var networkInterface in deviceType.Interfaces.ToChange)
                        {
                            Log.Logger.Here().Debug("     Queuing network interface changes for " + deviceType.Name);
                            var toChange = existingDeviceType.Interfaces.Where(x => x.Id == networkInterface.Id).FirstOrDefault();
                            if (toChange == null)
                                throw new Exception("Failed to find a network interface with ID " + networkInterface.Id + " on network device type " + existingDeviceType.Name);

                            if (networkInterface.Name != null) toChange.Name = networkInterface.Name;
                            if (networkInterface.InterfaceIndex.HasValue) toChange.InterfaceIndex = networkInterface.InterfaceIndex.Value;

                            dbContext.NetworkInterfaces.Update(toChange);
                        }
                    }
                }

                dbContext.NetworkDeviceTypes.Update(existingDeviceType);
            }

            return result;
        }

        private IEnumerable<string> ProcessTemplateChanges(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.Templates == null || changes.Templates.ToChange == null || changes.Templates.ToChange.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no templates specified to be changed");
                return result;
            }

            foreach (var template in changes.Templates.ToChange)
            {
                Log.Logger.Here().Debug("    Preparing TFTP " + template.Name + " to change");
                var existingTemplate = dbContext.Templates
                    .Where(x =>
                        x.Id == template.Id
                    )
                    .FirstOrDefault();

                if (existingTemplate == null)
                    throw new Exception("Failed to find template with id " + template.Id.ToString());

                existingTemplate.Name = template.Name;
                existingTemplate.Content = template.Content;

                dbContext.Templates.Update(existingTemplate);

                result.Add("    Queued template " + existingTemplate.Name + " to change");
            }

            return result;
        }

        private List<string> ProcessTFTPFileChanges(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.TFTPFiles == null || changes.TFTPFiles.ToChange == null || changes.TFTPFiles.ToChange.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no tftp files specified to be changed");
                return result;
            }

            foreach(var tftpFile in changes.TFTPFiles.ToChange)
            {
                Log.Logger.Here().Debug("    Preparing TFTP " + tftpFile.Name + " to change");
                var existingTftpFile = dbContext.TFTPFiles
                    .Where(x =>
                        x.Id == tftpFile.Id
                    )
                    .FirstOrDefault();

                if (existingTftpFile == null)
                    throw new Exception("Failed to find TFTP file with id " + tftpFile.Id.ToString());

                existingTftpFile.FilePath = tftpFile.Name;
                existingTftpFile.Content = tftpFile.Content;

                dbContext.TFTPFiles.Update(existingTftpFile);

                result.Add("    Queued TFTP " + existingTftpFile.FilePath + " to change");
            }

            return result;
        }

        private List<string> ProcessAdditions(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            //Log.Logger.Here().Debug("  Processing connection additions");
            //result.AddRange(ProcessConnectionAdditions(dbContext, changes));

            Log.Logger.Here().Debug("  Processing addition of network devices");
            result.AddRange(ProcessNetworkDeviceAdditions(dbContext, changes));

            Log.Logger.Here().Debug("  Processing addition of network device types");
            result.AddRange(ProcessNetworkDeviceTypeAdditions(dbContext, changes));

            Log.Logger.Here().Debug("  Processing addition of templates");
            result.AddRange(ProcessTemplateAdditions(dbContext, changes));

            Log.Logger.Here().Debug("  Processing addition of tftp files");
            result.AddRange(ProcessTFTPFileAdditions(dbContext, changes));

            return result;
        }

        private List<string> ProcessNetworkDeviceAdditions(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.NetworkDevices == null || changes.NetworkDevices.ToAdd == null || changes.NetworkDevices.ToAdd.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no network devices specified to be added");
                return result;
            }

            Log.Logger.Here().Debug("    Creating the network device objects specified to be added");
            var devicesToAdd =
                (
                    from device in changes.NetworkDevices.ToAdd
                    join deviceType in dbContext.NetworkDeviceTypes
                    on device.DeviceType equals deviceType.Name
                    select new NetworkDevice
                    {
                        Hostname = device.Hostname,
                        DomainName = device.DomainName,
                        DeviceType = deviceType,
                        Description = device.Description,
                        IPAddress = NetworkPrefix.Parse(device.IPAddress).Network.ToString(),
                        Network = NetworkPrefix.Parse(device.IPAddress).BaseNetwork.ToString(),
                        DHCPRelay = device.DHCPRelay,
                        DHCPTftpBootfile = device.DHCPTftpBootfile,
                        DHCPExclusions = (device.DHCPExclusions == null) ?
                            null :
                            (
                                from exclusion in device.DHCPExclusions
                                select new DHCPExclusion
                                {
                                    Start = exclusion.Start,
                                    End = exclusion.End
                                }
                            )
                            .ToList()
                    }
                )
                .ToList();

            Log.Logger.Here().Debug("    Creating the network device template configuration objects specified to be added");
            var templateConfigurationsToAdd =
                (
                    from device in changes.NetworkDevices.ToAdd
                    where device.Template != null
                    join template in dbContext.Templates
                    on device.Template.Name equals template.Name
                    join networkDevice in devicesToAdd
                    on (device.Hostname + '.' + device.DomainName) equals (networkDevice.Hostname + '.' + networkDevice.DomainName)
                    select new TemplateConfiguration
                    {
                        NetworkDevice = networkDevice,
                        Description = device.Template.Description,
                        Template = template,
                        Properties = (device.Template.Parameters == null) ?
                            null :
                            (
                                from parameter in device.Template.Parameters
                                select new TemplateProperty
                                {
                                    Name = parameter.Name,
                                    Value = parameter.Value
                                }
                            )
                            .ToList()
                    }
                )
                .ToList();

            Log.Logger.Here().Debug("    Queuing " + devicesToAdd.Count.ToString() + " network devices to be added");
            dbContext.NetworkDevices.AddRange(devicesToAdd);
            result.Add("    Queued " + devicesToAdd.Count.ToString() + " network devices to be added");

            Log.Logger.Here().Debug("    Queuing " + templateConfigurationsToAdd.Count.ToString() + " network device templates to be added");
            dbContext.TemplateConfigurations.AddRange(templateConfigurationsToAdd);
            result.Add("    Queued " + templateConfigurationsToAdd.Count.ToString() + " network device templates to be added");

            return result;
        }

        private List<string> ProcessNetworkDeviceTypeAdditions(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.DeviceTypes == null || changes.DeviceTypes.ToAdd == null || changes.DeviceTypes.ToAdd.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no network device types specified to be added");
                return result;
            }

            Log.Logger.Here().Debug("    Creating the network device type objects specified to be added");
            var deviceTypesToAdd =
                changes.DeviceTypes.ToAdd
                    .Select(x =>
                        new NetworkDeviceType
                        {
                            Name = x.Name,
                            Manufacturer = x.Manufacturer,
                            ProductId = x.ProductId,
                            Interfaces =
                                (x.Interfaces == null) ?
                                    null :
                                    GenerateInterfaces(x.Interfaces)
                        }
                    )
                    .ToList();

            Log.Logger.Here().Debug("    Queuing " + deviceTypesToAdd.Count.ToString() + " network device types to be added");
            dbContext.NetworkDeviceTypes.AddRange(deviceTypesToAdd);
            result.Add("    Queued " + deviceTypesToAdd.Count.ToString() + " network device types to be added");

            return result;
        }

        private List<NetworkInterface> GenerateInterfaces(List<NetworkInterfaceAddRangeViewModel> interfaces)
        {
            return interfaces
                .SelectMany(x =>
                    GenerateInterfaces(x)
                )
                .ToList();
        }

        private List<NetworkInterface> GenerateInterfaces(NetworkInterfaceAddRangeViewModel x)
        {
            var first = InterfaceName.tryParse(x.Start);

            var result = new List<NetworkInterface>();
            for (var i = 0; i < x.Count; i++)
                result.Add(new NetworkInterface
                {
                    Name = first.subsequent(i).ToString(),
                    InterfaceIndex = x.FirstIndex + i
                });

            return result;
        }

        private List<string> ProcessTemplateAdditions(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.Templates == null || changes.Templates.ToAdd == null || changes.Templates.ToAdd.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no templates specified to be added");
                return result;
            }

            Log.Logger.Here().Debug("    Loading the templates specified to be added");

            Log.Logger.Here().Debug("    Queuing " + changes.Templates.ToAdd.Count.ToString() + " to be added");
            dbContext.Templates.AddRange(
                changes.Templates.ToAdd
                    .Select(x =>
                        new Template
                        {
                            Name = x.Name,
                            Content = x.Content
                        }
                    )
                    .ToList()
                );
            result.Add("    Queued " + changes.Templates.ToAdd.Count.ToString() + " to be added");

            return result;
        }

        private List<string> ProcessTFTPFileAdditions(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.TFTPFiles == null || changes.TFTPFiles.ToAdd == null || changes.TFTPFiles.ToAdd.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no tftp files specified to be added");
                return result;
            }

            Log.Logger.Here().Debug("    Queuing " + changes.TFTPFiles.ToAdd.Count.ToString() + " tftp files to be added");
            dbContext.AddRange(
                changes.TFTPFiles.ToAdd
                    .Select(x =>
                        new TFTPFile
                        {
                            FilePath = x.Name,
                            Content = x.Content
                        }
                    )
                );
            result.Add("    Queued " + changes.TFTPFiles.ToAdd.Count.ToString() + " tftp files to be added");

            return result;
        }

        private async Task<List<string>> ProcessRemovals(
            PnPServerContext dbContext,
            BatchPutViewModel changes
        )
        {
            var result = new List<string>();

            Log.Logger.Here().Debug("  Processing removal of network devices");
            result.AddRange(await ProcessNetworkDeviceRemovals(dbContext, changes));

            Log.Logger.Here().Debug("  Processing removal of network device types");
            result.AddRange(await ProcessNetworkDeviceTypeRemovals(dbContext, changes));

            Log.Logger.Here().Debug("  Processing removal of templates");
            result.AddRange(await ProcessTemplateRemovals(dbContext, changes));

            Log.Logger.Here().Debug("  Processing removal of tftp files");
            result.AddRange(await ProcessTFTPFileRemovals(dbContext, changes));

            return result;
        }

        private async Task<IEnumerable<string>> ProcessTFTPFileRemovals(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.TFTPFiles == null || changes.TFTPFiles.ToRemove == null || changes.TFTPFiles.ToRemove.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no tftp files specified to be removed");
                return result;
            }

            Log.Logger.Here().Debug("    Loading the tftp files specified to be removed");
            var tftpFilesToRemove = await
                dbContext.TFTPFiles
                    .Where(x =>
                        changes.TFTPFiles.ToRemove.Contains(x.Id)
                    )
                    .ToListAsync();

            Log.Logger.Here().Debug("    Queuing " + tftpFilesToRemove.Count.ToString() + " to remove");
            dbContext.TFTPFiles.RemoveRange(tftpFilesToRemove);
            result.Add("    Queued " + tftpFilesToRemove.Count.ToString() + " to remove");

            return result;
        }

        private async Task<List<string>> ProcessTemplateRemovals(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.Templates == null || changes.Templates.ToRemove == null || changes.Templates.ToRemove.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no templates specified to be removed");
                return result;
            }

            Log.Logger.Here().Debug("    Loading the templates specified to be removed");
            var templatesToRemove = await
                dbContext.Templates
                    .Where(x =>
                        changes.Templates.ToRemove.Contains(x.Id)
                    )
                    .ToListAsync();

            Log.Logger.Here().Debug("    Queuing " + templatesToRemove.Count.ToString() + " to remove");
            dbContext.Templates.RemoveRange(templatesToRemove);
            result.Add("    Queued " + templatesToRemove.Count.ToString() + " to remove");

            return result;
        }

        private async Task<List<string>> ProcessNetworkDeviceTypeRemovals(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.DeviceTypes == null || changes.DeviceTypes.ToRemove == null || changes.DeviceTypes.ToRemove.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no network device types specified to be removed");
                return result;
            }

            Log.Logger.Here().Debug("    Loading the network device types specified to be removed");
            var deviceTypesToRemove = await
                dbContext.NetworkDeviceTypes
                    .Where(x =>
                        changes.DeviceTypes.ToRemove.Contains(x.Id)
                    )
                    .Include("Interfaces")
                    .ToListAsync();

            if (deviceTypesToRemove == null)
                throw new Exception("Failes to load the networking device types specified to be removed");

            Log.Logger.Here().Debug("    Building a manifest of networking interfaces to remove correlating to the network devices to remove");
            var interfacesToRemove =
                deviceTypesToRemove
                    .SelectMany(x =>
                        x.Interfaces
                    )
                    .ToList();

            if(interfacesToRemove != null && interfacesToRemove.Count > 0)
            {
                Log.Logger.Here().Debug("    Queuing " + interfacesToRemove.Count.ToString() + " interfaces to remove");
                dbContext.NetworkInterfaces.RemoveRange(interfacesToRemove);
                result.Add("    Queued " + interfacesToRemove.Count.ToString() + " interfaces to remove");
            }

            Log.Logger.Here().Debug("    Queuing " + deviceTypesToRemove.Count.ToString() + " device types to remove");
            dbContext.NetworkDeviceTypes.RemoveRange(deviceTypesToRemove);
            result.Add("    Queued " + deviceTypesToRemove.Count.ToString() + " device types to remove");

            return result;
        }

        private async Task<List<string>> ProcessNetworkDeviceRemovals(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if(changes.NetworkDevices == null || changes.NetworkDevices.ToRemove == null || changes.NetworkDevices.ToRemove.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no network devices specified to be removed");
                return result;
            }

            Log.Logger.Here().Debug("    Loading the network devices specified to be removed");
            var devicesToRemove = await
                dbContext.NetworkDevices
                    .Where(x =>
                        changes.NetworkDevices.ToRemove.Contains(x.Id)
                    )
                    .Include("DHCPExclusions")
                    .ToListAsync();

            if(devicesToRemove == null)
                throw new Exception("Failed to load networking devices specified to be removed");

            Log.Logger.Here().Debug("    Loading template configuration from devices which are to be removed");
            var templateConfigurationsToRemove = await
                dbContext.TemplateConfigurations
                    .Where(x =>
                        changes.NetworkDevices.ToRemove.Contains(x.NetworkDevice.Id)
                    )
                    .Include("Properties")
                    .ToListAsync();

            if (
                templateConfigurationsToRemove != null &&
                templateConfigurationsToRemove.Count > 0
            )
            {
                Log.Logger.Here().Debug("    Creating a list of template properties to remove as the template configurations are being removed");
                var templateParametersToRemove =
                    templateConfigurationsToRemove
                        .SelectMany(x =>
                            x.Properties
                        )
                        .ToList();
                if (
                    templateParametersToRemove != null &&
                    templateParametersToRemove.Count > 0
                )
                {
                    Log.Logger.Here().Debug("    Queuing " + templateParametersToRemove.Count().ToString() + " for removal");
                    dbContext.TemplateProperties.RemoveRange(templateParametersToRemove);
                    result.Add("    Queued " + templateParametersToRemove.Count().ToString() + " for removal");
                }

                Log.Logger.Here().Debug("    Queueing " + templateConfigurationsToRemove.Count.ToString() + " for removal");
                dbContext.RemoveRange(templateConfigurationsToRemove);
                result.Add("    Queued " + templateConfigurationsToRemove.Count.ToString() + " for removal");
            }

            // TODO : Remove DHCP pools for removed networks

            Log.Logger.Here().Debug("    Building a list of the DHCP exclusions to remove");
            var dhcpExclusions =
                devicesToRemove
                    .SelectMany(x =>
                        x.DHCPExclusions
                    )
                    .ToList();

            if (
                dhcpExclusions != null &&
                dhcpExclusions.Count > 0
            )
            {
                Log.Logger.Here().Debug("    Queuing " + dhcpExclusions.Count() + " for removal");
                dbContext.DhcpExclusions.RemoveRange(dhcpExclusions);
                result.Add("    Queued " + dhcpExclusions.Count() + " for removal");
            }

            Log.Logger.Here().Debug("    Queueing " + devicesToRemove.Count.ToString() + " for removal");
            dbContext.NetworkDevices.RemoveRange(devicesToRemove);
            result.Add("    Queued " + devicesToRemove.Count.ToString() + " for removal");

            return result;
        }

        private List<string> ProcessConnectionRemovals(PnPServerContext dbContext, BatchPutViewModel changes)
        {
            var result = new List<string>();

            if (changes.Connections == null || changes.Connections.ToRemove == null || changes.Connections.ToRemove.Count == 0)
            {
                Log.Logger.Here().Debug("   There are no connections specified to be removed");
                return result;
            }

            dbContext.NetworkDeviceLinks
                .RemoveRange(
                    dbContext.NetworkDeviceLinks
                        .Where(x => 
                            changes.Connections.ToRemove.Contains(x.Id)
                        )
                );
            Log.Logger.Here().Debug("   Queued removal of " + changes.Connections.ToRemove.Count + " connections");
            result.AddRange(
                    changes.Connections.ToRemove
                        .Select(x =>
                            "   Queued removal of connection with GUID " + x.ToString()
                        )
                        .ToList()
                );

            return result;
        }
    }
}
