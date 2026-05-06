using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripPassengersController : ControllerBase
{
    private readonly AppDbContext _context;

    public TripPassengersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripPassengerResponse>>> GetTripPassengers([FromQuery] int? tripId)
    {
        var query = _context.TripPassengers
            .AsNoTracking()
            .Include(tripPassenger => tripPassenger.Passenger)
            .Include(tripPassenger => tripPassenger.Trip)
            .AsQueryable();

        if (tripId.HasValue)
        {
            query = query.Where(tripPassenger => tripPassenger.TripId == tripId.Value);
        }

        var passengers = await query
            .OrderBy(tripPassenger => tripPassenger.TripId)
            .ThenBy(tripPassenger => tripPassenger.KoltukNo)
            .Select(tripPassenger => ToResponse(tripPassenger))
            .ToListAsync();

        return Ok(passengers);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TripPassengerResponse>> GetTripPassenger(int id)
    {
        var tripPassenger = await _context.TripPassengers
            .AsNoTracking()
            .Include(existing => existing.Passenger)
            .Include(existing => existing.Trip)
            .FirstOrDefaultAsync(existing => existing.Id == id);

        return tripPassenger is null ? NotFound() : Ok(ToResponse(tripPassenger));
    }

    [HttpPost]
    public async Task<ActionResult<TripPassengerResponse>> CreateTripPassenger(CreateTripPassengerRequest request)
    {
        var tripExists = await _context.Trips.AnyAsync(trip => trip.Id == request.TripId);
        if (!tripExists)
        {
            return BadRequest("Seçilen sefer bulunamadı.");
        }

        var passengerExists = await _context.Users.AnyAsync(user => user.Id == request.PassengerId && user.Role == "Passenger");
        if (!passengerExists)
        {
            return BadRequest("Seçilen yolcu bulunamadı veya rolü Passenger değil.");
        }

        var seatTaken = await _context.TripPassengers.AnyAsync(tripPassenger =>
            tripPassenger.TripId == request.TripId && tripPassenger.KoltukNo == request.KoltukNo);
        if (seatTaken)
        {
            return Conflict("Bu seferde seçilen koltuk dolu.");
        }

        var ticket = new TripPassenger
        {
            TripId = request.TripId,
            PassengerId = request.PassengerId,
            KoltukNo = request.KoltukNo,
            BiletAktifMi = request.BiletAktifMi
        };

        _context.TripPassengers.Add(ticket);
        await _context.SaveChangesAsync();

        var created = await _context.TripPassengers
            .AsNoTracking()
            .Include(existing => existing.Passenger)
            .Include(existing => existing.Trip)
            .FirstAsync(existing => existing.Id == ticket.Id);

        return CreatedAtAction(nameof(GetTripPassenger), new { id = ticket.Id }, ToResponse(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTripPassenger(int id, UpdateTripPassengerRequest request)
    {
        var ticket = await _context.TripPassengers.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        var seatTaken = await _context.TripPassengers.AnyAsync(tripPassenger =>
            tripPassenger.Id != id &&
            tripPassenger.TripId == request.TripId &&
            tripPassenger.KoltukNo == request.KoltukNo);
        if (seatTaken)
        {
            return Conflict("Bu seferde seçilen koltuk dolu.");
        }

        ticket.TripId = request.TripId;
        ticket.PassengerId = request.PassengerId;
        ticket.KoltukNo = request.KoltukNo;
        ticket.BiletAktifMi = request.BiletAktifMi;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTripPassenger(int id)
    {
        var ticket = await _context.TripPassengers.FindAsync(id);
        if (ticket is null)
        {
            return NotFound();
        }

        _context.TripPassengers.Remove(ticket);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static TripPassengerResponse ToResponse(TripPassenger tripPassenger) =>
        new(
            tripPassenger.Id,
            tripPassenger.TripId,
            tripPassenger.PassengerId,
            $"{tripPassenger.Passenger.Ad} {tripPassenger.Passenger.Soyad}",
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
    string KoltukNo,
    bool BiletAktifMi);
