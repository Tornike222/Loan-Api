using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoansApi.Services;
using System.Security.Claims;
using LoansApi.Domain.Entities;

namespace LoansApi.Api.Controllers;

[Authorize] 
[ApiController]
[Route("api/[controller]")]
public class AccountantController : ControllerBase
{
    private readonly IAccountantService _service;

    public AccountantController(IAccountantService service)
    {
        _service = service;
    }

    [HttpPatch("blockUser/{id}")]
    public async Task<IActionResult> BlockUser(int id)
    {
        var accountantName = User.FindFirstValue(ClaimTypes.Name);
        var accountantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        var requesterRole = Enum.Parse<UserRole>(roleClaim);
        try
        {
            var result = await _service.BlockUserAsync(id, accountantName, accountantId, requesterRole);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }

    [HttpPatch("unblockUser/{id}")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var accountantName = User.FindFirstValue(ClaimTypes.Name);
        var accountantId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirstValue(ClaimTypes.Role);
        var requesterRole = Enum.Parse<UserRole>(roleClaim);
        
        try
        {
            var result = await _service.UnblockUserAsync(id, accountantName, accountantId, requesterRole);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return StatusCode(403, ex.Message);
        }
    }
}