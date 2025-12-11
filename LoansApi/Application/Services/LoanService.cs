using LoansApi.Api.DTOs;
using LoansApi.Api.ResponseDTOs;
using LoansApi.Application.Helpers;
using LoansApi.Domain.Database;
using LoansApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NLog;
using ILogger = NLog.ILogger;

namespace LoansApi.Services;

public interface ILoanService
{
    Task<ServiceResponse<CreateLoanResponseDto>> CreateLoanAsync(int userId, CreateLoanDto dto);
    Task<ServiceResponse<bool>> UpdateLoanStatusAsync(int loanId, string status, UserRole requesterRole);
    Task<ServiceResponse<List<LoanDto>>> GetUserLoansAsync(int userId);
    Task<ServiceResponse<bool>> UpdateUserLoanAsync(int userId, UpdateLoanDto dto);
    Task<ServiceResponse<bool>> DeleteUserLoanAsync(int userId, int loanId);
    Task<ServiceResponse<List<LoanDto>>> GetAnyUserLoansAsync(UserRole requesterRole, int userId);
    Task<ServiceResponse<bool>> UpdateAnyUserLoanAsync(UserRole requesterRole, UpdateLoanDto dto);
    Task<ServiceResponse<bool>> DeleteAnyUserLoanAsync(UserRole requesterRole, int loanId);
}

public class LoanService : ILoanService
{
    private readonly LoanDbContext _ctx;
    private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

    public LoanService(LoanDbContext ctx)
    {
        _ctx = ctx;
    }

    public async Task<ServiceResponse<CreateLoanResponseDto>> CreateLoanAsync(int userId, CreateLoanDto dto)
    {
        var response = new ServiceResponse<CreateLoanResponseDto>();

        var user = await _ctx.Users.FindAsync(userId);
        if (user == null)
        {
            _logger.Warn("CreateLoan failed: User not found. UserId={0}", userId);
            response.Success = false;
            response.Message = "User not found.";
            return response;
        }

        if (user.IsBlocked)
        {
            _logger.Warn("Blocked user tried to create loan. UserId={0}", userId);
            response.Success = false;
            response.Message = "Blocked users cannot create loans.";
            return response;
        }

        if (!Enum.TryParse<LoanType>(dto.Type, true, out var loanType))
        {
            _logger.Warn("Invalid loan type. UserId={0}, Type={1}", userId, dto.Type);
            response.Success = false;
            response.Message = "Invalid loan type.";
            return response;
        }

        if (!Enum.TryParse<Currency>(dto.Currency, true, out var loanCurrency))
        {
            _logger.Warn("Invalid currency. UserId={0}, Currency={1}", userId, dto.Currency);
            response.Success = false;
            response.Message = "Invalid currency.";
            return response;
        }

        var loan = new Loan
        {
            UserId = userId,
            Amount = dto.Amount,
            Currency = loanCurrency,
            PeriodMonths = dto.PeriodMonths,
            Type = loanType,
            Status = LoanStatus.Processing,
            CreatedAt = DateTime.UtcNow
        };

        _ctx.Loans.Add(loan);
        await _ctx.SaveChangesAsync();

        _logger.Info("Loan created successfully. LoanId={0}, UserId={1}, Amount={2}", loan.Id, userId, dto.Amount);

        response.Data = new CreateLoanResponseDto
        {
            Id = loan.Id,
            Amount = loan.Amount,
            Currency = loan.Currency.ToString(),
            Status = loan.Status.ToString(),
        };

        response.Message = "Loan created successfully.";
        return response;
    }

    public async Task<ServiceResponse<bool>> UpdateLoanStatusAsync(int loanId, string status, UserRole requesterRole)
    {
        var response = new ServiceResponse<bool>();

        var loan = await _ctx.Loans.FindAsync(loanId);
        if (loan == null)
        {
            _logger.Warn("UpdateLoanStatus failed: Loan not found. LoanId={0}", loanId);
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        if (requesterRole != UserRole.Accountant)
        {
            _logger.Warn("Unauthorized status update attempt. Role={0}", requesterRole);
            response.Success = false;
            response.Message = "Only accountants can update loan status.";
            return response;
        }

        if (!Enum.TryParse<LoanStatus>(status, true, out var newStatus))
        {
            _logger.Warn("Invalid status update. LoanId={0}, Status={1}", loanId, status);
            response.Success = false;
            response.Message = "Invalid status.";
            return response;
        }

        loan.Status = newStatus;
        await _ctx.SaveChangesAsync();

        _logger.Info("Loan status updated. LoanId={0}, NewStatus={1}", loanId, status);

        response.Data = true;
        response.Message = $"Loan with LoanID {loanId} is updated to status {status}.";
        return response;
    }

    public async Task<ServiceResponse<List<LoanDto>>> GetUserLoansAsync(int userId)
    {
        var response = new ServiceResponse<List<LoanDto>>();

        var loans = await _ctx.Loans
            .Where(l => l.UserId == userId)
            .Select(l => new LoanDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                Amount = l.Amount,
                Currency = l.Currency.ToString(),
                PeriodMonths = l.PeriodMonths,
                Status = l.Status.ToString()
            })
            .ToListAsync();

        response.Data = loans;
        return response;
    }

    public async Task<ServiceResponse<bool>> UpdateUserLoanAsync(int userId, UpdateLoanDto dto)
    {
        var response = new ServiceResponse<bool>();

        var loan = await _ctx.Loans.FirstOrDefaultAsync(l => l.Id == dto.LoanId && l.UserId == userId);
        if (loan == null)
        {
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        if (loan.Status != LoanStatus.Processing)
        {
            response.Success = false;
            response.Message = "Only loans in 'Processing' status can be updated.";
            return response;
        }

        if (!Enum.TryParse<LoanType>(dto.Type, true, out var type))
        {
            response.Success = false;
            response.Message = "Invalid loan type.";
            return response;
        }

        if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
        {
            response.Success = false;
            response.Message = "Invalid currency.";
            return response;
        }

        loan.Type = type;
        loan.Amount = dto.Amount;
        loan.Currency = currency;
        loan.PeriodMonths = dto.PeriodMonths;

        await _ctx.SaveChangesAsync();

        response.Data = true;
        response.Message = "Loan updated successfully.";
        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteUserLoanAsync(int userId, int loanId)
    {
        var response = new ServiceResponse<bool>();

        var loan = await _ctx.Loans.FirstOrDefaultAsync(l => l.Id == loanId && l.UserId == userId);
        if (loan == null)
        {
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        if (loan.Status != LoanStatus.Processing)
        {
            response.Success = false;
            response.Message = "Only loans in 'Processing' status can be deleted.";
            return response;
        }

        _ctx.Loans.Remove(loan);
        await _ctx.SaveChangesAsync();

        response.Data = true;
        response.Message = "Loan deleted successfully.";
        return response;
    }
    
    // Accountant methods
    public async Task<ServiceResponse<List<LoanDto>>> GetAnyUserLoansAsync(UserRole requesterRole, int userId)
    {
        var response = new ServiceResponse<List<LoanDto>>();

        if (requesterRole != UserRole.Accountant)
        {
            response.Success = false;
            response.Message = "Only accountants can access this endpoint.";
            return response;
        }

        var loans = await _ctx.Loans
            .Where(l => l.UserId == userId)
            .Select(l => new LoanDto
            {
                Id = l.Id,
                Type = l.Type.ToString(),
                Amount = l.Amount,
                Currency = l.Currency.ToString(),
                PeriodMonths = l.PeriodMonths,
                Status = l.Status.ToString(),
                UserId = l.UserId,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        response.Data = loans;
        return response;
    }


public async Task<ServiceResponse<bool>> UpdateAnyUserLoanAsync(UserRole requesterRole, UpdateLoanDto dto)
{
    var response = new ServiceResponse<bool>();

    if (requesterRole != UserRole.Accountant)
    {
        response.Success = false;
        response.Message = "Only accountants can update loans.";
        return response;
    }

    var loan = await _ctx.Loans.FindAsync(dto.LoanId);
    if (loan == null)
    {
        response.Success = false;
        response.Message = "Loan not found.";
        return response;
    }

    if (!Enum.TryParse<LoanType>(dto.Type, true, out var type))
    {
        response.Success = false;
        response.Message = "Invalid loan type.";
        return response;
    }

    if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
    {
        response.Success = false;
        response.Message = "Invalid currency.";
        return response;
    }

    loan.Type = type;
    loan.Amount = dto.Amount;
    loan.Currency = currency;
    loan.PeriodMonths = dto.PeriodMonths;

    await _ctx.SaveChangesAsync();

    response.Data = true;
    response.Message = "Loan updated successfully.";
    return response;
}

public async Task<ServiceResponse<bool>> DeleteAnyUserLoanAsync(UserRole requesterRole, int loanId)
{
    var response = new ServiceResponse<bool>();

    if (requesterRole != UserRole.Accountant)
    {
        response.Success = false;
        response.Message = "Only accountants can delete loans.";
        return response;
    }

    var loan = await _ctx.Loans.FindAsync(loanId);
    if (loan == null)
    {
        response.Success = false;
        response.Message = "Loan not found.";
        return response;
    }

    _ctx.Loans.Remove(loan);
    await _ctx.SaveChangesAsync();

    response.Data = true;
    response.Message = "Loan deleted successfully.";
    return response;
}

}