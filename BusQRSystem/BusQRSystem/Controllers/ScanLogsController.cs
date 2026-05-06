using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ScanLogsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ScanLogsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ScanLogResponse>>> GetScanLogs([FromQuery] int? tripId)
    {
        var query = _context.ScanLogs
            .AsNoTracking()
            .Include(log => log.Passenger)
            .Include(log => log.ScannedByStaff)
            .AsQueryable();

        if (tripId.HasValue)
        {
            query = query.Where(log => log.TripId == tripId.Value);
        }

        var logs = await query
            .OrderByDescending(log => log.ScanTime)
            .Select(log => ToResponse(log))
            .ToListAsync();

        return Ok(logs);
    }

    [HttpPost("qr")]
    public async Task<ActionResult<ScanLogResponse>> CreateQrScan(CreateQrScanRequest request)
    {
        var passenger = await _context.Users.FirstOrDefaultAsync(user => user.QrCodeValue == request.QrCodeValue);
        if (passenger is null)
        {
            return NotFound("QR koduna ait yolcu bulunamadı.");
        }

        var hasActiveTicket = await _context.TripPassengers.AnyAsync(ticket =>
            ticket.TripId == request.TripId &&
            ticket.PassengerId == passenger.Id &&
            ticket.BiletAktifMi);
        if (!hasActiveTicket)
        {
            return BadRequest("Bu yolcunun seçilen sefer için aktif bileti yok.");
        }

        var staffExists = await _context.Users.AnyAsync(user =>
            user.Id == request.ScannedByStaffId &&
            (user.Role == "Staff" || user.Role == "Admin"));
        if (!staffExists)
        {
            return BadRequest("QR okutan personel bulunamadı veya yetkili değil.");
        }

        var log = new ScanLog
        {
            TripId = request.TripId,
            PassengerId = passenger.Id,
            ScannedByStaffId = request.ScannedByStaffId,
            ScanType = request.ScanType,
            LocationType = request.LocationType,
            ScanTime = DateTime.UtcNow
        };

        _context.ScanLogs.Add(log);
        await _context.SaveChangesAsync();

        var created = await _context.ScanLogs
            .AsNoTracking()
            .Include(existing => existing.Passenger)
            .Include(existing => existing.ScannedByStaff)
            .FirstAsync(existing => existing.Id == log.Id);

        return CreatedAtAction(nameof(GetScanLogs), new { tripId = request.TripId }, ToResponse(created));
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
