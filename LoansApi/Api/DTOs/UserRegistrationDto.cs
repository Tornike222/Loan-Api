namespace LoansApi.Api.DTOs;

public class UserRegistrationDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public int Age { get; set; }
    public decimal MonthlyIncome { get; set; }
    public string? Role { get; set; } 
    public string Password { get; set; }
}