namespace LoansApi.Domain.Entities;

public enum LoanType { Fast, Auto, Installment }
public enum LoanStatus { Processing, Approved, Rejected }

public class Loan
{
    public int Id { get; set; }
    public LoanType Type { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GEL";
    public int PeriodMonths { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Processing;
    public int UserId { get; set; } // FK
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}