using LoansApi.Domain.Entities;

namespace LoansApi.Api.ResponseDTOs;

public class UserRegistrationResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Role { get; set; }
}