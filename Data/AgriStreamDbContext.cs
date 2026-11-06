
using AgriStreamMVC.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace AgriStreamMVC.Data
{
    public class AgriStreamDbContext : DbContext
    {
        public AgriStreamDbContext(
            DbContextOptions<AgriStreamDbContext> options)
            : base(options)
        {
        }

        public DbSet<AppUser> AppUsers =>
            Set<AppUser>();

        public DbSet<Device> Devices =>
            Set<Device>();

        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<AppUser>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<Device>().HasData(
                new Device
                {
                    Id = 1,
                    DeviceName = "Pump-001",
                    OperationalStatus = "Idle"
                }
            );
        }
    }
}
