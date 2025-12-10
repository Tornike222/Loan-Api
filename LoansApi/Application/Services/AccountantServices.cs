using LoansApi.Api.DTOs;
using LoansApi.Domain.Database;
using Microsoft.EntityFrameworkCore;
using NLog;
using ILogger = NLog.ILogger;

namespace LoansApi.Services;

public interface IAccountantService
{
    Task<UserDto> BlockUserAsync(int id, string accountantName, string accountantId);
    Task<UserDto> UnblockUserAsync(int id, string accountantName, string accountantId);
}

public class AccountantService : IAccountantService
{
    private readonly LoanDbContext _ctx;
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public AccountantService(LoanDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<UserDto> BlockUserAsync(int id, string accountantName, string accountantId)
    {
        _logger.Info("BlockUserAsync called for userId={0}", id);

        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            _logger.Warn("Attempted to block non-existing user Id={0}", id);
            throw new KeyNotFoundException("User not found.");
        }

        if (user.IsBlocked)
        {
            _logger.Warn("User {0} (Id={1}) is already blocked.", user.Username, user.Id);
            throw new InvalidOperationException("User is already blocked.");
        }

        user.IsBlocked = true;
        await _ctx.SaveChangesAsync();

        _logger.Info(
            "User {0} (Id={1}) blocked by Accountant {2} (Id={3})",
            user.Username, user.Id, accountantName, accountantId
        );

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Age = user.Age,
            Role = user.Role.ToString(),
            MonthlyIncome = user.MonthlyIncome,
            IsBlocked = user.IsBlocked
        };
    }

    public async Task<UserDto> UnblockUserAsync(int id, string accountantName, string accountantId)
    {
        _logger.Info("UnblockUserAsync called for userId={0}", id);

        var user = await _ctx.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null)
        {
            _logger.Warn("Attempted to unblock non-existing user Id={0}", id);
            throw new KeyNotFoundException("User not found.");
        }

        if (!user.IsBlocked)
        {
            _logger.Warn("User {0} (Id={1}) is not blocked.", user.Username, user.Id);
            throw new InvalidOperationException("User is not blocked.");
        }

        user.IsBlocked = false;
        await _ctx.SaveChangesAsync();

        _logger.Info(
            "User {0} (Id={1}) unblocked by Accountant {2} (Id={3})",
            user.Username, user.Id, accountantName, accountantId
        );

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Age = user.Age,
            Role = user.Role.ToString(),
            MonthlyIncome = user.MonthlyIncome,
            IsBlocked = user.IsBlocked
        };
    }
}
