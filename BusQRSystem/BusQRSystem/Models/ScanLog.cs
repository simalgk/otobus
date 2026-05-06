namespace BusQRSystem.Models
{
    public class ScanLog
    {
        public int Id { get; set; }

        public int TripId { get; set; }
        public int PassengerId { get; set; }
        public int ScannedByStaffId { get; set; }

        public string ScanType { get; set; } = null!;
        // IN / OUT

        public string LocationType { get; set; } = null!;
        // OTOGAR / MOLA / VARIS

        public DateTime ScanTime { get; set; }

        // Navigation
        public Trip Trip { get; set; } = null!;
        public User Passenger { get; set; } = null!;
        public User ScannedByStaff { get; set; } = null!;
    }
}
