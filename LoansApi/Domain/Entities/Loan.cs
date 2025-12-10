namespace LoansApi.Domain.Entities;

public enum LoanType { Fast, Auto, Installment }
public enum LoanStatus { Processing, Approved, Rejected }
public enum Currency { GEL, USD }


public class Loan
{
    public int Id { get; set; }
    public LoanType Type { get; set; }
    public decimal Amount { get; set; }
    public Currency Currency { get; set; } = Currency.GEL;
    public int PeriodMonths { get; set; }
    public LoanStatus Status { get; set; } = LoanStatus.Processing;
    public int UserId { get; set; } // FK
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}