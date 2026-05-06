using BusQRSystem.Models;
using static BusQRSystem.Models.Buses;

namespace BusQRSystem.Data;

public class InMemoryBusStore
{
    private readonly object _lock = new();
    private int _nextBusId = 1;
    private int _nextTripId = 1;
    private int _nextUserId = 1;
    private int _nextTripPassengerId = 1;
    private int _nextScanLogId = 1;

    public List<Bus> Buses { get; } = [];
    public List<Trip> Trips { get; } = [];
    public List<User> Users { get; } = [];
    public List<TripPassenger> TripPassengers { get; } = [];
    public List<ScanLog> ScanLogs { get; } = [];

    public DemoSeedResult SeedDemo()
    {
        lock (_lock)
        {
            var bus = Buses.FirstOrDefault(item => item.Plaka == "32 QR 3421");
            if (bus is null)
            {
                bus = new Bus
                {
                    Id = _nextBusId++,
                    Plaka = "32 QR 3421",
                    FirmaAdi = "Isparta Turizm",
                    KoltukSayisi = 46
                };
                Buses.Add(bus);
            }

            var trip = Trips.FirstOrDefault(item =>
                item.BusId == bus.Id &&
                item.KalkisSehri == "Isparta" &&
                item.VarisSehri == "Istanbul" &&
                item.Durum != "Completed");

            if (trip is null)
            {
                trip = new Trip
                {
                    Id = _nextTripId++,
                    BusId = bus.Id,
                    Bus = bus,
                    KalkisSehri = "Isparta",
                    VarisSehri = "Istanbul",
                    KalkisSaati = DateTime.Today.AddHours(8).AddMinutes(30),
                    Durum = "Planned"
                };
                Trips.Add(trip);
                bus.Trips.Add(trip);
            }

            var staff = EnsureUser("Muavin", "Demo", "muavin@demo.local", "05550000000", "Staff", "STAFF-DEMO");

            var passengers = new[]
            {
                new DemoPassenger("Ayse", "Yilmaz", "12A", "QR-AYSE-12A"),
                new DemoPassenger("Mehmet", "Kaya", "12B", "QR-MEHMET-12B"),
                new DemoPassenger("Elif", "Demir", "13A", "QR-ELIF-13A"),
                new DemoPassenger("Ahmet", "Celik", "13B", "QR-AHMET-13B")
            };

            foreach (var item in passengers)
            {
                var passenger = EnsureUser(
                    item.Ad,
                    item.Soyad,
                    $"{item.QrCodeValue.ToLowerInvariant()}@demo.local",
                    "05551112233",
                    "Passenger",
                    item.QrCodeValue);

                var hasTicket = TripPassengers.Any(ticket =>
                    ticket.TripId == trip.Id && ticket.PassengerId == passenger.Id);
                if (hasTicket)
                {
                    continue;
                }

                var tripPassenger = new TripPassenger
                {
                    Id = _nextTripPassengerId++,
                    TripId = trip.Id,
                    Trip = trip,
                    PassengerId = passenger.Id,
                    Passenger = passenger,
                    KoltukNo = item.KoltukNo,
                    BiletAktifMi = true
                };
                TripPassengers.Add(tripPassenger);
                trip.TripPassengers.Add(tripPassenger);
                passenger.TripPassengers.Add(tripPassenger);
            }

            return new DemoSeedResult(trip.Id, staff.Id);
        }
    }

    public User AddUser(User user)
    {
        lock (_lock)
        {
            user.Id = _nextUserId++;
            Users.Add(user);
            return user;
        }
    }

    public Bus AddBus(Bus bus)
    {
        lock (_lock)
        {
            bus.Id = _nextBusId++;
            Buses.Add(bus);
            return bus;
        }
    }

    public Trip AddTrip(Trip trip)
    {
        lock (_lock)
        {
            trip.Id = _nextTripId++;
            trip.Bus = Buses.First(bus => bus.Id == trip.BusId);
            Trips.Add(trip);
            trip.Bus.Trips.Add(trip);
            return trip;
        }
    }

    public TripPassenger AddTripPassenger(TripPassenger tripPassenger)
    {
        lock (_lock)
        {
            tripPassenger.Id = _nextTripPassengerId++;
            tripPassenger.Trip = Trips.First(trip => trip.Id == tripPassenger.TripId);
            tripPassenger.Passenger = Users.First(user => user.Id == tripPassenger.PassengerId);
            TripPassengers.Add(tripPassenger);
            tripPassenger.Trip.TripPassengers.Add(tripPassenger);
            tripPassenger.Passenger.TripPassengers.Add(tripPassenger);
            return tripPassenger;
        }
    }

    public ScanLog AddScanLog(ScanLog log)
    {
        lock (_lock)
        {
            log.Id = _nextScanLogId++;
            log.Trip = Trips.First(trip => trip.Id == log.TripId);
            log.Passenger = Users.First(user => user.Id == log.PassengerId);
            log.ScannedByStaff = Users.First(user => user.Id == log.ScannedByStaffId);
            ScanLogs.Add(log);
            log.Trip.ScanLogs.Add(log);
            log.Passenger.PassengerScanLogs.Add(log);
            log.ScannedByStaff.StaffScanLogs.Add(log);
            return log;
        }
    }

    private User EnsureUser(string ad, string soyad, string email, string telefon, string role, string qrCodeValue)
    {
        var user = Users.FirstOrDefault(item => item.QrCodeValue == qrCodeValue);
        if (user is not null)
        {
            return user;
        }

        user = new User
        {
            Id = _nextUserId++,
            Ad = ad,
            Soyad = soyad,
            Email = email,
            Telefon = telefon,
            Role = role,
            QrCodeValue = qrCodeValue
        };
        Users.Add(user);
        return user;
    }

    private record DemoPassenger(string Ad, string Soyad, string KoltukNo, string QrCodeValue);
}

public record DemoSeedResult(int TripId, int StaffId);
