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
    [Authorize] 
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
        
        [HttpGet("my-loans")]
        public async Task<IActionResult> GetUserLoans()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _loanService.GetUserLoansAsync(userId);

            return Ok(result.Data);
        }

        [HttpPut("update")]
        public async Task<IActionResult> UpdateLoan([FromBody] UpdateLoanDto dto)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _loanService.UpdateUserLoanAsync(userId, dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteLoan([FromQuery] int loanId)
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var result = await _loanService.DeleteUserLoanAsync(userId, loanId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
        
        [HttpGet("all-loans")]
        public async Task<IActionResult> GetAnyUserLoans([FromQuery] int userId)
        {
            var requesterRole = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role));
            var result = await _loanService.GetAnyUserLoansAsync(requesterRole, userId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Data);
        }

        [HttpPut("update-any")]
        public async Task<IActionResult> UpdateAnyLoan([FromBody] UpdateLoanDto dto)
        {
            var requesterRole = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role));
            var result = await _loanService.UpdateAnyUserLoanAsync(requesterRole, dto);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }

        [HttpDelete("delete-any")]
        public async Task<IActionResult> DeleteAnyLoan([FromQuery] int loanId)
        {
            var requesterRole = Enum.Parse<UserRole>(User.FindFirstValue(ClaimTypes.Role));
            var result = await _loanService.DeleteAnyUserLoanAsync(requesterRole, loanId);

            if (!result.Success)
                return BadRequest(result.Message);

            return Ok(result.Message);
        }
    }
}