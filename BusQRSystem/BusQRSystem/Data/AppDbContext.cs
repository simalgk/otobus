using BusQRSystem.Models;
using Microsoft.EntityFrameworkCore;
using static BusQRSystem.Models.Buses;

namespace BusQRSystem.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Bus> Buses { get; set; }
        public DbSet<Trip> Trips { get; set; }
        public DbSet<TripPassenger> TripPassengers { get; set; }
        public DbSet<ScanLog> ScanLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Bus - Trip
            modelBuilder.Entity<Trip>()
                .HasOne(t => t.Bus)
                .WithMany(b => b.Trips)
                .HasForeignKey(t => t.BusId)
                .OnDelete(DeleteBehavior.Restrict);

            // TripPassenger - Trip
            modelBuilder.Entity<TripPassenger>()
                .HasOne(tp => tp.Trip)
                .WithMany(t => t.TripPassengers)
                .HasForeignKey(tp => tp.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // TripPassenger - Passenger(User)
            modelBuilder.Entity<TripPassenger>()
                .HasOne(tp => tp.Passenger)
                .WithMany(u => u.TripPassengers)
                .HasForeignKey(tp => tp.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ScanLog - Trip
            modelBuilder.Entity<ScanLog>()
                .HasOne(sl => sl.Trip)
                .WithMany(t => t.ScanLogs)
                .HasForeignKey(sl => sl.TripId)
                .OnDelete(DeleteBehavior.Restrict);

            // ScanLog - Passenger(User)
            modelBuilder.Entity<ScanLog>()
                .HasOne(sl => sl.Passenger)
                .WithMany(u => u.PassengerScanLogs)
                .HasForeignKey(sl => sl.PassengerId)
                .OnDelete(DeleteBehavior.Restrict);

            // ScanLog - Staff(User)
            modelBuilder.Entity<ScanLog>()
                .HasOne(sl => sl.ScannedByStaff)
                .WithMany(u => u.StaffScanLogs)
                .HasForeignKey(sl => sl.ScannedByStaffId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        }
}
