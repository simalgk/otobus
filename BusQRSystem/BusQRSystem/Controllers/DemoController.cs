using BusQRSystem.Data;
using Microsoft.AspNetCore.Mvc;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DemoController : ControllerBase
{
    private readonly InMemoryBusStore _store;

    public DemoController(InMemoryBusStore store)
    {
        _store = store;
    }

    [HttpPost("seed")]
    public ActionResult<DemoSeedResponse> Seed()
    {
        var result = _store.SeedDemo();
        return Ok(new DemoSeedResponse(result.TripId, result.StaffId));
    }
}

public record DemoSeedResponse(int TripId, int StaffId);
