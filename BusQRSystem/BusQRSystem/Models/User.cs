namespace BusQRSystem.Models
{
    public class User
    {


        public int Id { get; set; }

        public string Ad { get; set; } = null!;
        public string Soyad { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Telefon { get; set; } = null!;

        public string Role { get; set; } = null!;
        // Passenger, Staff, Admin

        public string QrCodeValue { get; set; } = null!;

        // Navigation
        public ICollection<TripPassenger> TripPassengers { get; set; } = new List<TripPassenger>();
        public ICollection<ScanLog> PassengerScanLogs { get; set; } = new List<ScanLog>();
        public ICollection<ScanLog> StaffScanLogs { get; set; } = new List<ScanLog>();


    }
}
