using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TripsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TripsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TripResponse>>> GetTrips()
    {
        var trips = await _context.Trips
            .AsNoTracking()
            .Include(trip => trip.Bus)
            .OrderByDescending(trip => trip.KalkisSaati)
            .Select(trip => ToResponse(trip))
            .ToListAsync();

        return Ok(trips);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<TripDetailResponse>> GetTrip(int id)
    {
        var trip = await _context.Trips
            .AsNoTracking()
            .Include(trip => trip.Bus)
            .Include(trip => trip.TripPassengers)
            .ThenInclude(tripPassenger => tripPassenger.Passenger)
            .FirstOrDefaultAsync(trip => trip.Id == id);

        if (trip is null)
        {
            return NotFound();
        }

        var passengers = trip.TripPassengers
            .OrderBy(tripPassenger => tripPassenger.KoltukNo)
            .Select(tripPassenger => new TripPassengerSummary(
                tripPassenger.Id,
                tripPassenger.PassengerId,
                $"{tripPassenger.Passenger.Ad} {tripPassenger.Passenger.Soyad}",
                tripPassenger.KoltukNo,
                tripPassenger.BiletAktifMi))
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
    public async Task<ActionResult<TripResponse>> CreateTrip(CreateTripRequest request)
    {
        var busExists = await _context.Buses.AnyAsync(bus => bus.Id == request.BusId);
        if (!busExists)
        {
            return BadRequest("Seçilen otobüs bulunamadı.");
        }

        var trip = new Trip
        {
            BusId = request.BusId,
            KalkisSehri = request.KalkisSehri,
            VarisSehri = request.VarisSehri,
            KalkisSaati = request.KalkisSaati,
            VarisSaati = request.VarisSaati,
            Durum = request.Durum
        };

        _context.Trips.Add(trip);
        await _context.SaveChangesAsync();

        var created = await _context.Trips
            .AsNoTracking()
            .Include(existing => existing.Bus)
            .FirstAsync(existing => existing.Id == trip.Id);

        return CreatedAtAction(nameof(GetTrip), new { id = trip.Id }, ToResponse(created));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateTrip(int id, UpdateTripRequest request)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip is null)
        {
            return NotFound();
        }

        var busExists = await _context.Buses.AnyAsync(bus => bus.Id == request.BusId);
        if (!busExists)
        {
            return BadRequest("Seçilen otobüs bulunamadı.");
        }

        trip.BusId = request.BusId;
        trip.KalkisSehri = request.KalkisSehri;
        trip.VarisSehri = request.VarisSehri;
        trip.KalkisSaati = request.KalkisSaati;
        trip.VarisSaati = request.VarisSaati;
        trip.Durum = request.Durum;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteTrip(int id)
    {
        var trip = await _context.Trips.FindAsync(id);
        if (trip is null)
        {
            return NotFound();
        }

        _context.Trips.Remove(trip);
        await _context.SaveChangesAsync();
        return NoContent();
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
