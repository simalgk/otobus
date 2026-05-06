using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<UserResponse>>> GetUsers()
    {
        var users = await _context.Users
            .AsNoTracking()
            .OrderBy(user => user.Ad)
            .ThenBy(user => user.Soyad)
            .Select(user => ToResponse(user))
            .ToListAsync();

        return Ok(users);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<UserResponse>> GetUser(int id)
    {
        var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == id);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    [HttpPost]
    public async Task<ActionResult<UserResponse>> CreateUser(CreateUserRequest request)
    {
        if (await _context.Users.AnyAsync(user => user.Email == request.Email))
        {
            return Conflict("Bu e-posta adresi zaten kayıtlı.");
        }

        var user = new User
        {
            Ad = request.Ad,
            Soyad = request.Soyad,
            Email = request.Email,
            Telefon = request.Telefon,
            Role = request.Role,
            QrCodeValue = string.IsNullOrWhiteSpace(request.QrCodeValue)
                ? Guid.NewGuid().ToString("N")
                : request.QrCodeValue
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToResponse(user));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserRequest request)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        var emailTaken = await _context.Users.AnyAsync(existing => existing.Id != id && existing.Email == request.Email);
        if (emailTaken)
        {
            return Conflict("Bu e-posta adresi başka bir kullanıcıya ait.");
        }

        user.Ad = request.Ad;
        user.Soyad = request.Soyad;
        user.Email = request.Email;
        user.Telefon = request.Telefon;
        user.Role = request.Role;
        user.QrCodeValue = request.QrCodeValue;

        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null)
        {
            return NotFound();
        }

        _context.Users.Remove(user);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Ad, user.Soyad, user.Email, user.Telefon, user.Role, user.QrCodeValue);
}

public record CreateUserRequest(
    string Ad,
    string Soyad,
    string Email,
    string Telefon,
    string Role,
    string? QrCodeValue);

public record UpdateUserRequest(
    string Ad,
    string Soyad,
    string Email,
    string Telefon,
    string Role,
    string QrCodeValue);

public record UserResponse(
    int Id,
    string Ad,
    string Soyad,
    string Email,
    string Telefon,
    string Role,
    string QrCodeValue);
