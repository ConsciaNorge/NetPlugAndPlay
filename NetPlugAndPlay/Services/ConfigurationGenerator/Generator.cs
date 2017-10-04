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

            if(templateConfig == null)
            {
                // TODO : Throw
                return string.Empty;
            }

            string template = templateConfig.Template.Content;

            var context = new VelocityContext();
            foreach (var prop in templateConfig.Properties)
            {
                context.Put(prop.Name, prop.Value);
            }

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
