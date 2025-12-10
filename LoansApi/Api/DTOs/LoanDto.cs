namespace LoansApi.Api.DTOs;

public class LoanDto
{
    public Guid Id { get; set; }
    public string Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public int PeriodMonths { get; set; }
    public string Status { get; set; }
    public Guid UserId { get; set; }
    public DateTime CreatedAt { get; set; }
}