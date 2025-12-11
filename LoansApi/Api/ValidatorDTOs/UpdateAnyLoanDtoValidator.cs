using FluentValidation;
using LoansApi.Api.DTOs;

namespace LoansApi.Api.ValidatorDTOs;

public class UpdateAnyLoanDtoValidator : AbstractValidator<UpdateAnyLoanDto>
{
    public UpdateAnyLoanDtoValidator()
    {
        RuleFor(x => x.LoanId)
            .GreaterThan(0)
            .WithMessage("LoanId must be greater than 0.");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Loan type is required.")
            .Must(t => new[] { "Fast", "Auto", "Installment" }
                .Contains(t, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Loan type must be 'Fast', 'Auto', or 'Installment'.");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Amount must be greater than 0.");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Must(c => new[] { "GEL", "USD" }
                .Contains(c, StringComparer.OrdinalIgnoreCase))
            .WithMessage("Currency must be 'GEL' or 'USD'.");

        RuleFor(x => x.PeriodMonths)
            .GreaterThan(0).WithMessage("PeriodMonths must be greater than 0.");
    }
}
