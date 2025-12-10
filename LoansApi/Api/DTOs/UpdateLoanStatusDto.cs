namespace LoansApi.Api.DTOs;

public class UpdateLoanStatusDto
{    
    public int LoanId { get; set; }
    public string Status { get; set; } // "Processing", "Approved", "Rejected"
}