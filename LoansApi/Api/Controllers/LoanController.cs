using LoansApi.Api.DTOs;
using LoansApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NLog;
using System.Security.Claims;
using LoansApi.Domain.Entities;
using ILogger = NLog.ILogger;

namespace LoansApi.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoanController : ControllerBase
    {
        private readonly ILoanService _loanService;
        private static readonly ILogger _logger = LogManager.GetCurrentClassLogger();

        public LoanController(ILoanService loanService)
        {
            _loanService = loanService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateLoan(CreateLoanDto dto)
        {
            var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _loanService.CreateLoanAsync(userId, dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }
        
        [HttpPatch("updateStatus")]
        public async Task<IActionResult> UpdateLoanStatus(UpdateLoanStatusDto dto)
        {
            var roleClaim = User.FindFirstValue(ClaimTypes.Role);
            var requesterRole = Enum.Parse<UserRole>(roleClaim);

            var result = await _loanService.UpdateLoanStatusAsync(dto.LoanId, dto.Status, requesterRole);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
    }
}