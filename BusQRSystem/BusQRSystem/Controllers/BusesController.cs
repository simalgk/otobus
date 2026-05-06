using BusQRSystem.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using static BusQRSystem.Models.Buses;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusesController : ControllerBase
{
    private readonly AppDbContext _context;

    public BusesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<BusResponse>>> GetBuses()
    {
        var buses = await _context.Buses
            .AsNoTracking()
            .OrderBy(bus => bus.FirmaAdi)
            .ThenBy(bus => bus.Plaka)
            .Select(bus => ToResponse(bus))
            .ToListAsync();

        return Ok(buses);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<BusResponse>> GetBus(int id)
    {
        var bus = await _context.Buses.AsNoTracking().FirstOrDefaultAsync(bus => bus.Id == id);
        return bus is null ? NotFound() : Ok(ToResponse(bus));
    }

    [HttpPost]
    public async Task<ActionResult<BusResponse>> CreateBus(CreateBusRequest request)
    {
        if (await _context.Buses.AnyAsync(bus => bus.Plaka == request.Plaka))
        {
            return Conflict("Bu plakaya ait otobüs zaten kayıtlı.");
        }

        var bus = new Bus
        {
            Plaka = request.Plaka,
            FirmaAdi = request.FirmaAdi,
            KoltukSayisi = request.KoltukSayisi
        };

        _context.Buses.Add(bus);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetBus), new { id = bus.Id }, ToResponse(bus));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateBus(int id, UpdateBusRequest request)
    {
        var bus = await _context.Buses.FindAsync(id);
        if (bus is null)
        {
            return NotFound();
        }

        var plateTaken = await _context.Buses.AnyAsync(existing => existing.Id != id && existing.Plaka == request.Plaka);
        if (plateTaken)
        {
            return Conflict("Bu plakaya ait başka bir otobüs var.");
        }

        bus.Plaka = request.Plaka;
        bus.FirmaAdi = request.FirmaAdi;
        bus.KoltukSayisi = request.KoltukSayisi;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteBus(int id)
    {
        var bus = await _context.Buses.FindAsync(id);
        if (bus is null)
        {
            return NotFound();
        }

        _context.Buses.Remove(bus);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static BusResponse ToResponse(Bus bus) =>
        new(bus.Id, bus.Plaka, bus.FirmaAdi, bus.KoltukSayisi);
}

public record CreateBusRequest(string Plaka, string FirmaAdi, int KoltukSayisi);

public record UpdateBusRequest(string Plaka, string FirmaAdi, int KoltukSayisi);

public record BusResponse(int Id, string Plaka, string FirmaAdi, int KoltukSayisi);
