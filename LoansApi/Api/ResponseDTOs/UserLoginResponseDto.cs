using LoansApi.Domain.Entities;

namespace LoansApi.Api.ResponseDTOs;

public class UserLoginResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
    public string Token { get; set; }
}