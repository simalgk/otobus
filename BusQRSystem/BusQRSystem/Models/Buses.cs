namespace BusQRSystem.Models
{
    public class Buses
    {
        public class Bus
        {
            public int Id { get; set; }

            public string Plaka { get; set; } = null!;
            public string FirmaAdi { get; set; } = null!;
            public int KoltukSayisi { get; set; }

            // Navigation
            public ICollection<Trip> Trips { get; set; } = new List<Trip>();
        }
    }
}
