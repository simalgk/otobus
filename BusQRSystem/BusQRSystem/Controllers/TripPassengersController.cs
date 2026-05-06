using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripPassengersController : ControllerBase
{
    private readonly InMemoryBusStore _store;

    public TripPassengersController(InMemoryBusStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<TripPassengerResponse>> GetTripPassengers([FromQuery] int? tripId)
    {
        _store.SeedDemo();
        var query = _store.TripPassengers.AsEnumerable();
        if (tripId.HasValue)
        {
            query = query.Where(item => item.TripId == tripId.Value);
        }

        return Ok(query
            .OrderBy(item => item.TripId)
            .ThenBy(item => item.KoltukNo)
            .Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public ActionResult<TripPassengerResponse> GetTripPassenger(int id)
    {
        var ticket = _store.TripPassengers.FirstOrDefault(item => item.Id == id);
        return ticket is null ? NotFound() : Ok(ToResponse(ticket));
    }

    [HttpPost]
    public ActionResult<TripPassengerResponse> CreateTripPassenger(CreateTripPassengerRequest request)
    {
        if (_store.Trips.All(trip => trip.Id != request.TripId))
        {
            return BadRequest("Seçilen sefer bulunamadı.");
        }

        if (_store.Users.All(user => user.Id != request.PassengerId))
        {
            return BadRequest("Seçilen yolcu bulunamadı.");
        }

        var ticket = _store.AddTripPassenger(new TripPassenger
        {
            TripId = request.TripId,
            PassengerId = request.PassengerId,
            KoltukNo = request.KoltukNo,
            BiletAktifMi = request.BiletAktifMi
        });

        return CreatedAtAction(nameof(GetTripPassenger), new { id = ticket.Id }, ToResponse(ticket));
    }

    private static TripPassengerResponse ToResponse(TripPassenger tripPassenger) =>
        new(
            tripPassenger.Id,
            tripPassenger.TripId,
            tripPassenger.PassengerId,
            $"{tripPassenger.Passenger.Ad} {tripPassenger.Passenger.Soyad}",
            tripPassenger.Passenger.QrCodeValue,
            tripPassenger.KoltukNo,
            tripPassenger.BiletAktifMi);
}

public record CreateTripPassengerRequest(int TripId, int PassengerId, string KoltukNo, bool BiletAktifMi);

public record UpdateTripPassengerRequest(int TripId, int PassengerId, string KoltukNo, bool BiletAktifMi);

public record TripPassengerResponse(
    int Id,
    int TripId,
    int PassengerId,
    string PassengerName,
    string QrCodeValue,
    string KoltukNo,
    bool BiletAktifMi);
