using static BusQRSystem.Models.Buses;

namespace BusQRSystem.Models
{
    public class Trip
    {
        public int Id { get; set; }

        public int BusId { get; set; }

        public string KalkisSehri { get; set; } = null!;
        public string VarisSehri { get; set; } = null!;
        public DateTime KalkisSaati { get; set; }
        public DateTime? VarisSaati { get; set; }

        public string Durum { get; set; } = null!;
        // Planned, Started, Completed, Cancelled

        // Navigation
        public Bus Bus { get; set; } = null!;
        public ICollection<TripPassenger> TripPassengers { get; set; } = new List<TripPassenger>();
        public ICollection<ScanLog> ScanLogs { get; set; } = new List<ScanLog>();
    }
}
