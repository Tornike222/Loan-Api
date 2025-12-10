using LoansApi.Api.DTOs;
using LoansApi.Api.ResponseDTOs;
using LoansApi.Application.Helpers;
using LoansApi.Domain.Database;
using LoansApi.Domain.Entities;
using NLog;
using ILogger = NLog.ILogger;

namespace LoansApi.Services;

public interface ILoanService
{
    Task<ServiceResponse<CreateLoanResponseDto>> CreateLoanAsync(int userId, CreateLoanDto dto);
    Task<ServiceResponse<bool>> UpdateLoanStatusAsync(int loanId, string status, UserRole requesterRole);

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

}
