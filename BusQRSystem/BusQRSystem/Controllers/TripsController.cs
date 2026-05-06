using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly InMemoryBusStore _store;

    public TripsController(InMemoryBusStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TripResponse>> GetTrips()
    {
        _store.SeedDemo();
        return Ok(_store.Trips
            .OrderByDescending(trip => trip.KalkisSaati)
            .Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public ActionResult<TripDetailResponse> GetTrip(int id)
    {
        var trip = _store.Trips.FirstOrDefault(item => item.Id == id);
        if (trip is null)
        {
            return NotFound();
        }

        var passengers = _store.TripPassengers
            .Where(item => item.TripId == id)
            .OrderBy(item => item.KoltukNo)
            .Select(item => new TripPassengerSummary(
                item.Id,
                item.PassengerId,
                $"{item.Passenger.Ad} {item.Passenger.Soyad}",
                item.KoltukNo,
                item.BiletAktifMi))
            .ToList();

        return Ok(new TripDetailResponse(
            trip.Id,
            trip.BusId,
            trip.Bus.Plaka,
            trip.Bus.FirmaAdi,
            trip.KalkisSehri,
            trip.VarisSehri,
            trip.KalkisSaati,
            trip.VarisSaati,
            trip.Durum,
            passengers));
    }

    [HttpPost]
    public ActionResult<TripResponse> CreateTrip(CreateTripRequest request)
    {
        if (_store.Buses.All(bus => bus.Id != request.BusId))
        {
            return BadRequest("Seçilen otobüs bulunamadı.");
        }

        var trip = _store.AddTrip(new Trip
        {
            BusId = request.BusId,
            KalkisSehri = request.KalkisSehri,
            VarisSehri = request.VarisSehri,
            KalkisSaati = request.KalkisSaati,
            VarisSaati = request.VarisSaati,
            Durum = request.Durum
        });

        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, ToResponse(trip));
    }

    private static TripResponse ToResponse(Trip trip) =>
        new(
            trip.Id,
            trip.BusId,
            trip.Bus.Plaka,
            trip.Bus.FirmaAdi,
            trip.KalkisSehri,
            trip.VarisSehri,
            trip.KalkisSaati,
            trip.VarisSaati,
            trip.Durum);
}

public record CreateTripRequest(
    int BusId,
    string KalkisSehri,
    string VarisSehri,
    DateTime KalkisSaati,
    DateTime? VarisSaati,
    string Durum);

public record UpdateTripRequest(
    int BusId,
    string KalkisSehri,
    string VarisSehri,
    DateTime KalkisSaati,
    DateTime? VarisSaati,
    string Durum);

public record TripResponse(
    int Id,
    int BusId,
    string Plaka,
    string FirmaAdi,
    string KalkisSehri,
    string VarisSehri,
    DateTime KalkisSaati,
    DateTime? VarisSaati,
    string Durum);

public record TripDetailResponse(
    int Id,
    int BusId,
    string Plaka,
    string FirmaAdi,
    string KalkisSehri,
    string VarisSehri,
    DateTime KalkisSaati,
    DateTime? VarisSaati,
    string Durum,
    IReadOnlyCollection<TripPassengerSummary> Passengers);

public record TripPassengerSummary(
    int Id,
    int PassengerId,
    string PassengerName,
    string KoltukNo,
    bool BiletAktifMi);
