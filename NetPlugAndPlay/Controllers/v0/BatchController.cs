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

            Log.Logger.Here().Debug(" Processing removals from incoming changes");
            try
            {
                result.AddRange(await ProcessRemovals(dbContext, changes));
            }
            catch(Exception e)
            {
                Log.Logger.Here().Debug(e, "Failed to queue removal of objects per request");
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

        private async Task<List<string>> ProcessRemovals(
            PnPServerContext dbContext,
            BatchPutViewModel changes
        )
        {
            var result = new List<string>();

            Log.Logger.Here().Debug("  Processing connection removals");
            result.AddRange(ProcessConnectionRemovals(dbContext, changes));

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
                Log.Logger.Here().Debug("   There are no tftp files specificed to be removed");
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
                Log.Logger.Here().Debug("   There are no templates specificed to be removed");
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
                Log.Logger.Here().Debug("   There are no network device types specificed to be removed");
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

            if(interfacesToRemove == null && interfacesToRemove.Count > 0)
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
                Log.Logger.Here().Debug("   There are no network devices specificed to be removed");
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
