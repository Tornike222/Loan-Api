namespace LoansApi.Api.ResponseDTOs;

public class CreateLoanResponseDto
{
    public int Id { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "GEL";
    public string Status { get; set; }
}