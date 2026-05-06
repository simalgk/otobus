namespace BusQRSystem.Models
{
    public class TripPassenger
    {
       
        
            public int Id { get; set; }

            public int TripId { get; set; }
            public int PassengerId { get; set; }

            public string KoltukNo { get; set; } = null!;
            public bool BiletAktifMi { get; set; }

            // Navigation
            public Trip Trip { get; set; } = null!;
            public User Passenger { get; set; } = null!;
        
    }
}
