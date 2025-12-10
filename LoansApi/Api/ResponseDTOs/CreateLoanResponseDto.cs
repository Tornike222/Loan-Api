namespace LoansApi.Api.ResponseDTOs;

public class LoanResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GEL";
    public int PeriodMonths { get; set; }
    public string Status { get; set; }
}