using Microsoft.EntityFrameworkCore;

namespace NetPlugAndPlay.Models
{
    public class PnPServerContext : DbContext
    {
        public PnPServerContext(DbContextOptions<PnPServerContext> options) : base(options)
{
            //any changes to the context options can now be done here
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NetworkDeviceLink>()
                .HasOne(p => p.NetworkDevice)
                .WithMany(b => b.Uplinks);
        }

        public DbSet<NetworkDevice> NetworkDevices { get; set; }
        public DbSet<NetworkDeviceLink> NetworkDeviceLinks { get; set; }
        public DbSet<NetworkDeviceType> NetworkDeviceTypes { get; set; }
        public DbSet<NetworkInterface> NetworkInterfaces { get; set; }
        public DbSet<Template> Templates { get; set; }
        public DbSet<TemplateConfiguration> TemplateConfigurations { get; set; }
        public DbSet<TemplateProperty> TemplateProperties { get; set; }
    }
}
