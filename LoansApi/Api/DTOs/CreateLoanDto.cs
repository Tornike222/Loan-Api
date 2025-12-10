namespace LoansApi.Api.DTOs;

public class CreateLoanDto
{
    public string Type { get; set; }   // "Fast", "Auto", "Installment"
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public int PeriodMonths { get; set; }
}