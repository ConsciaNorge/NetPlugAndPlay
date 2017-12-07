using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SpaServices.Webpack;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetPlugAndPlay.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog;

namespace NetPlugAndPlay
{
    public class Startup
    {
        static Services.TFTP_Server.Server tftpServer;
        static Services.DHCP_Server.Server dhcpServer;
        static Services.SyslogServer.SyslogServer syslogServer;
        static Services.DeviceConfigurator.DeviceConfigurator deviceConfigurator;

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        static public IConfiguration Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            services.AddDbContext<PnPServerContext>(options =>
                            options.UseSqlServer(Configuration.GetValue<string>("Data:DefaultConnection:ConnectionString")));
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            SampleData.InitializeDatabaseAsync(app.ApplicationServices).Wait();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebpackDevMiddleware(new WebpackDevMiddlewareOptions
                {
                    HotModuleReplacement = true
                });
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            Log.Logger = new LoggerConfiguration()
                //.MinimumLevel.Debug()
                //.MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                //.Enrich.FromLogContext()
                //.WriteTo.Console()
                //.WriteTo.Debug()
                .ReadFrom.Configuration(Configuration)
                .CreateLogger();

                        tftpServer = new Services.TFTP_Server.Server();
                        dhcpServer = new Services.DHCP_Server.Server();
                        syslogServer = new Services.SyslogServer.SyslogServer();
                        deviceConfigurator = new Services.DeviceConfigurator.DeviceConfigurator();

                        syslogServer.OnSyslogMessage += deviceConfigurator.SyslogMessageHandler;
                        dhcpServer.OnIPReleased += deviceConfigurator.ForgetIP;


            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");

                routes.MapSpaFallbackRoute(
                    name: "spa-fallback",
                    defaults: new { controller = "Home", action = "Index" });
            });
        }
    }
}
