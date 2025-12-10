using LoansApi.Api.DTOs;
using LoansApi.Api.ResponseDTOs;
using LoansApi.Domain.Database;
using LoansApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;
using System.Security.Cryptography;
using System.Text;
using ILogger = NLog.ILogger;

namespace LoansApi.Services;

public interface IUserService
{
    Task<UserRegistrationResponseDto> RegisterAsync(UserRegistrationDto dto);
    Task<UserLoginResponseDto> LoginAsync(LoginDto dto);
    Task<UserDto?> GetUserByIdAsync(int id, int requesterId, UserRole requesterRole);
}

public class UserService : IUserService
{
    private readonly LoanDbContext _ctx;
    private readonly IAuthService _auth;
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public UserService(LoanDbContext ctx, IAuthService auth)
    {
        _ctx = ctx;
        _auth = auth;
    }

    public async Task<UserRegistrationResponseDto> RegisterAsync(UserRegistrationDto dto)
    {
        _logger.Info("Register attempt: {0}", dto.Username);

        if (await _ctx.Users.AnyAsync(u => u.Username == dto.Username))
        {
            _logger.Warn("Registration failed: Username taken.");
            throw new InvalidOperationException("Username already taken.");
        }

        if (await _ctx.Users.AnyAsync(u => u.Email == dto.Email))
        {
            _logger.Warn("Registration failed: Email taken.");
            throw new InvalidOperationException("Email already taken.");
        }

        UserRole role = UserRole.User;
        if (!string.IsNullOrWhiteSpace(dto.Role) &&
            Enum.TryParse<UserRole>(dto.Role, true, out var parsedRole))
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

        _logger.Info("User registered successfully: {0}", dto.Username);

        return new UserRegistrationResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString()
        };
    }

    public async Task<UserLoginResponseDto> LoginAsync(LoginDto dto)
    {
        _logger.Info("Login attempt: {0}", dto.Username);

        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Username == dto.Username)
            ?? throw new UnauthorizedAccessException("Invalid username or password.");

        if (!VerifyPassword(dto.Password, user.PasswordHash))
        {
            _logger.Warn("Invalid password for: {0}", dto.Username);
            throw new UnauthorizedAccessException("Invalid username or password.");
        }

        _logger.Info("Login success: {0}", dto.Username);

        return new UserLoginResponseDto
        {
            Id = user.Id,
            Username = user.Username,
            Role = user.Role.ToString(),
            Token = _auth.GenerateToken(user)
        };
    }

    public async Task<UserDto?> GetUserByIdAsync(int id, int requesterId, UserRole requesterRole)
    {
        _logger.Info("GetUserById called: {0}", id);

        if (requesterRole != UserRole.Accountant && requesterId != id)
        {
            _logger.Warn("Forbidden access attempt by {0}", requesterId);
            throw new UnauthorizedAccessException("You can only access your own data.");
        }

        var user = await _ctx.Users
            .Where(u => u.Id == id)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                Role = u.Role.ToString(),
                FirstName = u.FirstName,
                LastName = u.LastName,
                Age = u.Age,
                MonthlyIncome = u.MonthlyIncome
            })
            .FirstOrDefaultAsync();

        _logger.Info("GetUserById success: {0}", id);

        return user;
    }

    #region Password Hashing

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

    #endregion
}
