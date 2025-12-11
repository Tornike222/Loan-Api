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
        _logger.Debug("CreateLoanAsync called. UserId={0}", userId);

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
            _logger.Warn("Blocked user attempted loan creation. UserId={0}", userId);
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

        try
        {
            _ctx.Loans.Add(loan);
            await _ctx.SaveChangesAsync();

            _logger.Info("Loan created successfully. LoanId={0}, UserId={1}", loan.Id, userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error while creating loan. UserId={0}", userId);
            response.Success = false;
            response.Message = "Internal server error.";
            return response;
        }

        response.Data = new CreateLoanResponseDto
        {
            Id = loan.Id,
            Amount = loan.Amount,
            Currency = loan.Currency.ToString(),
            Status = loan.Status.ToString()
        };

        response.Message = "Loan created successfully.";
        return response;
    }

    public async Task<ServiceResponse<bool>> UpdateLoanStatusAsync(int loanId, string status, UserRole requesterRole)
    {
        _logger.Debug("UpdateLoanStatusAsync called. LoanId={0}, RequestedStatus={1}, Role={2}", loanId, status,
            requesterRole);

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
            _logger.Warn("Invalid loan status provided. LoanId={0}, Status={1}", loanId, status);
            response.Success = false;
            response.Message = "Invalid status.";
            return response;
        }

        loan.Status = newStatus;

        try
        {
            await _ctx.SaveChangesAsync();
            _logger.Info("Loan status updated. LoanId={0}, NewStatus={1}", loanId, status);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating loan status. LoanId={0}", loanId);
            response.Success = false;
            response.Message = "Internal server error.";
            return response;
        }

        response.Data = true;
        response.Message = $"Loan status updated to {status}.";
        return response;
    }

    public async Task<ServiceResponse<List<LoanDto>>> GetUserLoansAsync(int userId)
    {
        _logger.Debug("GetUserLoansAsync called. UserId={0}", userId);

        var response = new ServiceResponse<List<LoanDto>>();

        try
        {
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

            _logger.Info("Fetched {0} loans for UserId={1}", loans.Count, userId);
            response.Data = loans;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error fetching loans for UserId={0}", userId);
            response.Success = false;
            response.Message = "Internal server error.";
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> UpdateUserLoanAsync(int userId, UpdateLoanDto dto)
    {
        _logger.Debug("UpdateUserLoanAsync called. UserId={0}, LoanId={1}", userId, dto.LoanId);

        var response = new ServiceResponse<bool>();

        var loan = await _ctx.Loans.FirstOrDefaultAsync(l => l.Id == dto.LoanId && l.UserId == userId);
        if (loan == null)
        {
            _logger.Warn("UpdateUserLoan failed: Loan not found. LoanId={0}, UserId={1}", dto.LoanId, userId);
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        if (loan.Status != LoanStatus.Processing)
        {
            _logger.Warn("User tried to update non-processing loan. LoanId={0}, UserId={1}", dto.LoanId, userId);
            response.Success = false;
            response.Message = "Only Processing loans can be updated.";
            return response;
        }

        if (!Enum.TryParse<LoanType>(dto.Type, true, out var type))
        {
            _logger.Warn("Invalid loan type. LoanId={0}, Type={1}", dto.LoanId, dto.Type);
            response.Success = false;
            response.Message = "Invalid loan type.";
            return response;
        }

        if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
        {
            _logger.Warn("Invalid currency. LoanId={0}, Currency={1}", dto.LoanId, dto.Currency);
            response.Success = false;
            response.Message = "Invalid currency.";
            return response;
        }

        loan.Type = type;
        loan.Amount = dto.Amount;
        loan.Currency = currency;
        loan.PeriodMonths = dto.PeriodMonths;

        try
        {
            await _ctx.SaveChangesAsync();
            _logger.Info("Loan updated by user. LoanId={0}, UserId={1}", dto.LoanId, userId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating user loan. LoanId={0}", dto.LoanId);
            response.Success = false;
            response.Message = "Internal server error.";
            return response;
        }

        response.Data = true;
        response.Message = "Loan updated successfully.";
        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteUserLoanAsync(int userId, int loanId)
    {
        _logger.Debug("DeleteUserLoanAsync called. UserId={0}, LoanId={1}", userId, loanId);

        var response = new ServiceResponse<bool>();

        var loan = await _ctx.Loans.FirstOrDefaultAsync(l => l.Id == loanId && l.UserId == userId);
        if (loan == null)
        {
            _logger.Warn("DeleteUserLoan failed: Loan not found. UserId={0}, LoanId={1}", userId, loanId);
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        if (loan.Status != LoanStatus.Processing)
        {
            _logger.Warn("User attempted to delete non-processing loan. LoanId={0}, UserId={1}", loanId, userId);
            response.Success = false;
            response.Message = "Only Processing loans can be deleted.";
            return response;
        }

        try
        {
            _ctx.Loans.Remove(loan);
            await _ctx.SaveChangesAsync();

            _logger.Info("Loan deleted by user. UserId={0}, LoanId={1}", userId, loanId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deleting user loan. LoanId={0}", loanId);
            response.Success = false;
            response.Message = "Internal server error.";
            return response;
        }

        response.Data = true;
        response.Message = "Loan deleted successfully.";
        return response;
    }

    // Accountant methods

    public async Task<ServiceResponse<List<LoanDto>>> GetAnyUserLoansAsync(UserRole requesterRole, int userId)
    {
        _logger.Debug("GetAnyUserLoansAsync called. RequestedUserId={0}, Role={1}", userId, requesterRole);

        var response = new ServiceResponse<List<LoanDto>>();

        if (requesterRole != UserRole.Accountant)
        {
            _logger.Warn("Unauthorized attempt to fetch any user loans. Role={0}", requesterRole);
            response.Success = false;
            response.Message = "Only accountants can access this endpoint.";
            return response;
        }

        try
        {
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

            _logger.Info("Accountant fetched {0} loans for UserId={1}", loans.Count, userId);

            response.Data = loans;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error fetching any-user loans. UserId={0}", userId);
            response.Success = false;
            response.Message = "Internal server error.";
        }

        return response;
    }

    public async Task<ServiceResponse<bool>> UpdateAnyUserLoanAsync(UserRole requesterRole, UpdateLoanDto dto)
    {
        _logger.Debug("UpdateAnyUserLoanAsync called. LoanId={0}, Role={1}", dto.LoanId, requesterRole);

        var response = new ServiceResponse<bool>();

        if (requesterRole != UserRole.Accountant)
        {
            _logger.Warn("Unauthorized loan update attempt by non-accountant. Role={0}", requesterRole);
            response.Success = false;
            response.Message = "Only accountants can update loans.";
            return response;
        }

        var loan = await _ctx.Loans.FindAsync(dto.LoanId);
        if (loan == null)
        {
            _logger.Warn("UpdateAnyUserLoan failed: Loan not found. LoanId={0}", dto.LoanId);
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        if (!Enum.TryParse<LoanType>(dto.Type, true, out var type))
        {
            _logger.Warn("Invalid loan type in accountant update. LoanId={0}, Type={1}", dto.LoanId, dto.Type);
            response.Success = false;
            response.Message = "Invalid loan type.";
            return response;
        }

        if (!Enum.TryParse<Currency>(dto.Currency, true, out var currency))
        {
            _logger.Warn("Invalid currency in accountant update. LoanId={0}, Currency={1}", dto.LoanId, dto.Currency);
            response.Success = false;
            response.Message = "Invalid currency.";
            return response;
        }

        loan.Type = type;
        loan.Amount = dto.Amount;
        loan.Currency = currency;
        loan.PeriodMonths = dto.PeriodMonths;

        try
        {
            await _ctx.SaveChangesAsync();
            _logger.Info("Loan updated by accountant. LoanId={0}", dto.LoanId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error updating loan as accountant. LoanId={0}", dto.LoanId);
            response.Success = false;
            response.Message = "Internal server error.";
            return response;
        }

        response.Data = true;
        response.Message = "Loan updated successfully.";
        return response;
    }

    public async Task<ServiceResponse<bool>> DeleteAnyUserLoanAsync(UserRole requesterRole, int loanId)
    {
        _logger.Debug("DeleteAnyUserLoanAsync called. LoanId={0}, Role={1}", loanId, requesterRole);

        var response = new ServiceResponse<bool>();

        if (requesterRole != UserRole.Accountant)
        {
            _logger.Warn("Unauthorized loan delete attempt by non-accountant. Role={0}", requesterRole);
            response.Success = false;
            response.Message = "Only accountants can delete loans.";
            return response;
        }

        var loan = await _ctx.Loans.FindAsync(loanId);
        if (loan == null)
        {
            _logger.Warn("DeleteAnyUserLoan failed: Loan not found. LoanId={0}", loanId);
            response.Success = false;
            response.Message = "Loan not found.";
            return response;
        }

        try
        {
            _ctx.Loans.Remove(loan);
            await _ctx.SaveChangesAsync();
            _logger.Info("Loan deleted by accountant. LoanId={0}", loanId);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deleting loan by accountant. LoanId={0}", loanId);
            response.Success = false;
            response.Message = "Internal server error.";
            return response;
        }

        response.Data = true;
        response.Message = "Loan deleted successfully.";
        return response;
    }
}