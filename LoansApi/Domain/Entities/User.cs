namespace LoansApi.Domain.Entities;

public class User
{
    public int Id { get; set; } 
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Username { get; set; } = null!;
    public int Age { get; set; }
    public string Email { get; set; } = null!;
    public decimal MonthlyIncome { get; set; }
    public bool IsBlocked { get; set; } = false;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; } = UserRole.User; 
    public ICollection<Loan> Loans { get; set; } = new List<Loan>();
}

public enum UserRole
{
    User,
    Accountant
}