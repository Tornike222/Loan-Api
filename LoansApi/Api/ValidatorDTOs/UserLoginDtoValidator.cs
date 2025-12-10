using FluentValidation;
using LoansApi.Api.DTOs;

public class UserLoginDtoValidator : AbstractValidator<LoginDto>
{
    public UserLoginDtoValidator()
    {
        RuleFor(x => x.Username).NotEmpty().WithMessage("Username is required.");
        RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
    }
}