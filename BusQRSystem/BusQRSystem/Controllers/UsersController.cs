using BusQRSystem.Data;
using BusQRSystem.Models;
using Microsoft.AspNetCore.Mvc;

namespace BusQRSystem.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly InMemoryBusStore _store;

    public UsersController(InMemoryBusStore store)
    {
        _store = store;
    }

    [HttpGet]
    public ActionResult<IEnumerable<UserResponse>> GetUsers()
    {
        _store.SeedDemo();
        return Ok(_store.Users
            .OrderBy(user => user.Ad)
            .ThenBy(user => user.Soyad)
            .Select(ToResponse));
    }

    [HttpGet("{id:int}")]
    public ActionResult<UserResponse> GetUser(int id)
    {
        var user = _store.Users.FirstOrDefault(item => item.Id == id);
        return user is null ? NotFound() : Ok(ToResponse(user));
    }

    [HttpPost]
    public ActionResult<UserResponse> CreateUser(CreateUserRequest request)
    {
        if (_store.Users.Any(user => user.Email == request.Email))
        {
            return Conflict("Bu e-posta adresi zaten kayıtlı.");
        }

        var user = _store.AddUser(new User
        {
            Ad = request.Ad,
            Soyad = request.Soyad,
            Email = request.Email,
            Telefon = request.Telefon,
            Role = request.Role,
            QrCodeValue = string.IsNullOrWhiteSpace(request.QrCodeValue)
                ? Guid.NewGuid().ToString("N")
                : request.QrCodeValue
        });

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, ToResponse(user));
    }

    [HttpPut("{id:int}")]
    public IActionResult UpdateUser(int id, UpdateUserRequest request)
    {
        var user = _store.Users.FirstOrDefault(item => item.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.Ad = request.Ad;
        user.Soyad = request.Soyad;
        user.Email = request.Email;
        user.Telefon = request.Telefon;
        user.Role = request.Role;
        user.QrCodeValue = request.QrCodeValue;
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public IActionResult DeleteUser(int id)
    {
        var user = _store.Users.FirstOrDefault(item => item.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        _store.Users.Remove(user);
        return NoContent();
    }

    private static UserResponse ToResponse(User user) =>
        new(user.Id, user.Ad, user.Soyad, user.Email, user.Telefon, user.Role, user.QrCodeValue);
}

public record CreateUserRequest(string Ad, string Soyad, string Email, string Telefon, string Role, string? QrCodeValue);

public record UpdateUserRequest(string Ad, string Soyad, string Email, string Telefon, string Role, string QrCodeValue);

public record UserResponse(int Id, string Ad, string Soyad, string Email, string Telefon, string Role, string QrCodeValue);
