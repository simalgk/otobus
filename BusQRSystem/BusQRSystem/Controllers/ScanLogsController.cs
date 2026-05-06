using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanLogsController : ControllerBase
{
    private readonly InMemoryBusStore _store;

    public ScanLogsController(InMemoryBusStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<ScanLogResponse>> GetScanLogs([FromQuery] int? tripId)
    {
        var query = _store.ScanLogs.AsEnumerable();
        if (tripId.HasValue)
        {
            query = query.Where(log => log.TripId == tripId.Value);
        }

        return Ok(query.OrderByDescending(log => log.ScanTime).Select(ToResponse));
    }

    [HttpPost("qr")]
    public ActionResult<ScanLogResponse> CreateQrScan(CreateQrScanRequest request)
    {
        var passenger = _store.Users.FirstOrDefault(user => user.QrCodeValue == request.QrCodeValue);
        if (passenger is null)
        {
            return NotFound("QR koduna ait yolcu bulunamadı.");
        }

        var hasActiveTicket = _store.TripPassengers.Any(ticket =>
            ticket.TripId == request.TripId &&
            ticket.PassengerId == passenger.Id &&
            ticket.BiletAktifMi);
        if (!hasActiveTicket)
        {
            return BadRequest("Bu yolcunun seçilen sefer için aktif bileti yok.");
        }

        var staff = _store.Users.FirstOrDefault(user =>
            user.Id == request.ScannedByStaffId &&
            (user.Role == "Staff" || user.Role == "Admin"));
        if (staff is null)
        {
            return BadRequest("QR okutan personel bulunamadı veya yetkili değil.");
        }

        var log = _store.AddScanLog(new ScanLog
        {
            TripId = request.TripId,
            PassengerId = passenger.Id,
            ScannedByStaffId = staff.Id,
            ScanType = request.ScanType,
            LocationType = request.LocationType,
            ScanTime = DateTime.UtcNow
        });

        return CreatedAtAction(nameof(GetScanLogs), new { tripId = request.TripId }, ToResponse(log));
    }

    private static ScanLogResponse ToResponse(ScanLog log) =>
        new(
            log.Id,
            log.TripId,
            log.PassengerId,
            $"{log.Passenger.Ad} {log.Passenger.Soyad}",
            log.ScannedByStaffId,
            $"{log.ScannedByStaff.Ad} {log.ScannedByStaff.Soyad}",
            log.ScanType,
            log.LocationType,
            log.ScanTime);
}

public record CreateQrScanRequest(
    int TripId,
    string QrCodeValue,
    int ScannedByStaffId,
    string ScanType,
    string LocationType);

public record ScanLogResponse(
    int Id,
    int TripId,
    int PassengerId,
    string PassengerName,
    int ScannedByStaffId,
    string ScannedByStaffName,
    string ScanType,
    string LocationType,
    DateTime ScanTime);
