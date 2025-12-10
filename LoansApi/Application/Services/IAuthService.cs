using LoansApi.Domain.Entities;

namespace LoansApi.Services;

public interface IAuthService
{
    string GenerateToken(User user);
}