using BusQRSystem.Data;
using Microsoft.AspNetCore.Mvc;
using static BusQRSystem.Models.Buses;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BusesController : ControllerBase
{
    private readonly InMemoryBusStore _store;

    public BusesController(InMemoryBusStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<BusResponse>> GetBuses()
    {
        _store.SeedDemo();
        return Ok(_store.Buses.OrderBy(bus => bus.Plaka).Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public ActionResult<BusResponse> GetBus(int id)
    {
        var bus = _store.Buses.FirstOrDefault(item => item.Id == id);
        return bus is null ? NotFound() : Ok(ToResponse(bus));
    }

    [HttpPost]
    public ActionResult<BusResponse> CreateBus(CreateBusRequest request)
    {
        var bus = _store.AddBus(new Bus
        {
            Plaka = request.Plaka,
            FirmaAdi = request.FirmaAdi,
            KoltukSayisi = request.KoltukSayisi
        });

        return CreatedAtAction(nameof(GetBus), new { id = bus.Id }, ToResponse(bus));
    }

    private static BusResponse ToResponse(Bus bus) =>
        new(bus.Id, bus.Plaka, bus.FirmaAdi, bus.KoltukSayisi);
}

public record CreateBusRequest(string Plaka, string FirmaAdi, int KoltukSayisi);

public record UpdateBusRequest(string Plaka, string FirmaAdi, int KoltukSayisi);

public record BusResponse(int Id, string Plaka, string FirmaAdi, int KoltukSayisi);
