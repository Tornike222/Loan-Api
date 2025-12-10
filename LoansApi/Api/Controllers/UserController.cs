using System.Security.Claims;
using LoansApi.Api.DTOs;
using LoansApi.Domain.Database;
using LoansApi.Domain.Entities;
using LoansApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using LoansApi.Api.ResponseDTOs;
using Microsoft.AspNetCore.Authorization;

namespace LoansApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly LoanDbContext _ctx;
    private readonly IAuthService _auth;

    public UserController(LoanDbContext ctx, IAuthService auth)
    {
        _ctx = ctx;
        _auth = auth;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register(UserRegistrationDto dto)
    {
        var usernameExists = await _ctx.Users.AnyAsync(u => u.Username == dto.Username);
        if (usernameExists)
            return BadRequest("Username already taken.");
        
        var userEmailExists = await _ctx.Users.AnyAsync(u => u.Email == dto.Email);
        if (userEmailExists)
            return BadRequest("Email already taken.");
        
        var allowedRoles = new[] { UserRole.User, UserRole.Accountant };
        UserRole role = UserRole.User;
        if (!string.IsNullOrWhiteSpace(dto.Role) &&
            Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole) &&
            allowedRoles.Contains(parsedRole))
        {
            role = parsedRole;
        }

        var user = new User
        {
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            Username = dto.Username,
            Email = dto.Email,
            Age = dto.Age,
            MonthlyIncome = dto.MonthlyIncome,
            Role = role,
            PasswordHash = HashPassword(dto.Password)
        };

        _ctx.Users.Add(user);
        await _ctx.SaveChangesAsync();

        var response = new UserRegistrationResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString()
        };

        return Ok(response);
    }
    
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null)
            return Unauthorized("Invalid username or password.");

        if (!VerifyPassword(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid username or password.");
        
        var response = new UserLoginResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            Token = _auth.GenerateToken(user)
        };
        
        return Ok(response);
    }

    [HttpGet("userInfo/{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(int id)
    {
        // Get the logged-in user's id from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var loggedInUserRole = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role));

        if (userIdClaim == null)
            return Unauthorized("User not logged in.");

        if (!int.TryParse(userIdClaim, out int loggedInUserId))
            return Unauthorized("Invalid token.");
        
        if (loggedInUserRole != UserRole.Accountant && loggedInUserId != id)
            return StatusCode(403, "You can only access your own data.");

        var user = await _ctx.Users
            .Where(u => u.Id == id)
            .Select(u => new 
            {
                u.Id,
                u.Username,
                u.Email,
                Role = u.Role.ToString(),
                u.FirstName,
                u.LastName,
                u.Age,
                u.MonthlyIncome
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound("User not found.");

        return Ok(user);
    }

    private string HashPassword(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    private bool VerifyPassword(string password, string hash)
    {
        return HashPassword(password) == hash;
    }
}
