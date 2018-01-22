using libnetworkutility;
using Microsoft.EntityFrameworkCore;
using NetPlugAndPlay.Models;
using NVelocity;
using NVelocity.App;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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

            var dhcpRelayDevices = await dbContext.NetworkDevices
                .Where(x =>
                    x.DHCPRelay
                )
                .ToListAsync();

            var address = IPAddress.Parse(ipAddress);
            var relayDevice = dhcpRelayDevices
                .Where(x =>
                    NetworkPrefix.Parse(x.Network).Contains(address)
                )
                .FirstOrDefault();

            var context = new VelocityContext();
            foreach (var prop in templateConfig.Properties)
                context.Put(prop.Name, prop.Value);

            context.Put("ipAddress", ipAddress);
            context.Put("hostname", templateConfig.NetworkDevice.Hostname);
            context.Put("domainName", templateConfig.NetworkDevice.DomainName);
            context.Put("tftpServer", address.GetSourceIP());
            context.Put("dhcpServer", address.GetSourceIP());
            context.Put("syslogServer", address.GetSourceIP());

            context.Put("telnetUsername", DeviceConfigurator.DeviceConfigurator.TelnetUsername);
            context.Put("telnetPassword", DeviceConfigurator.DeviceConfigurator.TelnetPassword);
            context.Put("enablePassword", DeviceConfigurator.DeviceConfigurator.TelnetEnablePassword);

            context.Put("deviceReadyMessage", DeviceConfigurator.DeviceConfigurator.DeviceConfiguredLogMessage);

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

            context.Put("telnetUsername", DeviceConfigurator.DeviceConfigurator.TelnetUsername);
            context.Put("telnetPassword", DeviceConfigurator.DeviceConfigurator.TelnetPassword);
            context.Put("ipAddress", templateConfig.NetworkDevice.IPAddress.ToString());

            var thisServerIP = Common.NetworkTools.LocalRoutingTable.QueryRoutingInterface(IPAddress.Parse(templateConfig.NetworkDevice.IPAddress));

            context.Put("tftpServer", thisServerIP.ToString());
            context.Put("automationServer", thisServerIP.ToString());
            context.Put("dhcpServer", thisServerIP.ToString());
            context.Put("syslogServer", thisServerIP.ToString());
            context.Put("deviceReadyMessage", DeviceConfigurator.DeviceConfigurator.DeviceConfiguredLogMessage);

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
                    NetworkPrefix.Parse(x.Network).Contains(address)
                )
                .FirstOrDefault();


            string template = tftpFile.Content;

            var context = new VelocityContext();

            context.Put("ipAddress", ipAddress);
            context.Put("hostname", string.Join("", address.GetAddressBytes().Select(x => x.ToString("X2")).ToList()));
            if (relayDevice != null)
                context.Put("domainName", relayDevice.DomainName);
            context.Put("tftpServer", address.GetSourceIP());
            context.Put("dhcpServer", address.GetSourceIP());
            context.Put("syslogServer", address.GetSourceIP());

            context.Put("telnetUsername", DeviceConfigurator.DeviceConfigurator.TelnetUsername);
            context.Put("telnetPassword", DeviceConfigurator.DeviceConfigurator.TelnetPassword);
            context.Put("enablePassword", DeviceConfigurator.DeviceConfigurator.TelnetEnablePassword);

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
