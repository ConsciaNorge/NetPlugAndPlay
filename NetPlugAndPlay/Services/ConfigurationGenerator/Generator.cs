using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;
using NVelocity;
using NVelocity.App;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using IPAddressExtensions;

namespace NetPlugAndPlay.Services.ConfigurationGenerator
{
    public class Generator
    {
        public async static Task<string> Generate(
                string ipAddress,
                PnPServerContext dbContext
            )
        {
            var templateConfig = await dbContext.TemplateConfigurations
                .Include("Template")
                .Include("NetworkDevice")
                .Include("Properties")
                .Where(x => x.NetworkDevice.IPAddress == ipAddress)
                .FirstOrDefaultAsync();

            if (templateConfig == null)
            {
                // TODO : Throw
                return string.Empty;
            }

            string template = templateConfig.Template.Content;

            var context = new VelocityContext();
            foreach (var prop in templateConfig.Properties)
                context.Put(prop.Name, prop.Value);

            context.Put("ipAddress", ipAddress);

            context.Put("esc", new NVelocityRuntime.CiscoEsc());

            var engine = new VelocityEngine();
            engine.Init();

            var outputWriter = new StringWriter();
            engine.Evaluate(context, outputWriter, "eval1", template);
            var templateText = outputWriter.GetStringBuilder().ToString();

            return templateText;
        }

        public async static Task<string> Generate(
                Guid networkDeviceId,
                PnPServerContext dbContext
            )
        {
            var templateConfig = await dbContext.TemplateConfigurations
                .Include("Template")
                .Include("NetworkDevice")
                .Include("Properties")
                .Where(x => x.NetworkDevice.Id == networkDeviceId)
                .FirstOrDefaultAsync();

            if (templateConfig == null)
            {
                // TODO : Throw
                return string.Empty;
            }

            string template = templateConfig.Template.Content;

            var context = new VelocityContext();
            foreach (var prop in templateConfig.Properties)
                context.Put(prop.Name, prop.Value);

            context.Put("ipAddress", templateConfig.NetworkDevice.IPAddress.ToString());

            context.Put("esc", new NVelocityRuntime.CiscoEsc());

            var engine = new VelocityEngine();
            engine.Init();

            var outputWriter = new StringWriter();
            engine.Evaluate(context, outputWriter, "eval1", template);
            var templateText = outputWriter.GetStringBuilder().ToString();

            return templateText;
        }

        public async static Task<string> Generate(
                string ipAddress,
                string filePath,
                PnPServerContext dbContext
            )
        {
            var tftpFile = await dbContext.TFTPFiles
                .Where(x =>
                    x.FilePath == filePath
                )
                .FirstOrDefaultAsync();

            if (tftpFile == null)
                return string.Empty;

            var dhcpRelayDevices = await dbContext.NetworkDevices
                .Where(x =>
                    x.DHCPRelay
                )
                .ToListAsync();

            var address = IPAddress.Parse(ipAddress);
            var relayDevice = dhcpRelayDevices
                .Where(x =>
                    LibDHCPServer.VolatilePool.NetworkPrefix.Parse(x.Network).Contains(address)
                )
                .FirstOrDefault();


            string template = tftpFile.Content;

            var context = new VelocityContext();

            context.Put("ipAddress", ipAddress);
            context.Put("hostname", string.Join("", address.GetAddressBytes().Select(x => x.ToString("X2")).ToList()));
            if (relayDevice != null)
                context.Put("domainName", relayDevice.DomainName);
            context.Put("tftpServer", address.SourceIP());
            context.Put("deviceReadyMessage", DeviceConfigurator.DeviceConfigurator.DeviceConfiguredLogMessage);

            context.Put("esc", new NVelocityRuntime.CiscoEsc());

            var engine = new VelocityEngine();
            engine.Init();

            var outputWriter = new StringWriter();
            engine.Evaluate(context, outputWriter, "eval1", template);
            var templateText = outputWriter.GetStringBuilder().ToString();

            return templateText;
        }
    }
}
