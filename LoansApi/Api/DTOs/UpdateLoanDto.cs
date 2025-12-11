namespace LoansApi.Api.DTOs;

public class UpdateLoanDto
{
    public int LoanId { get; set; }
    public string Type { get; set; }   // "Fast", "Auto", "Installment"
    public decimal Amount { get; set; }
    public string Currency { get; set; } // "GEL", "USD"
    public int PeriodMonths { get; set; }
}