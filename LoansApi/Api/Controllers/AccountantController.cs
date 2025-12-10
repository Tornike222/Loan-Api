using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LoansApi.Services;
using System.Security.Claims;

namespace LoansApi.Api.Controllers;

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
    [Authorize(Roles = "Accountant")]
    public async Task<IActionResult> BlockUser(int id)
    {
        var accountantName = User.FindFirstValue(ClaimTypes.Name);
        var accountantId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var result = await _service.BlockUserAsync(id, accountantName, accountantId);
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
    [Authorize(Roles = "Accountant")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var accountantName = User.FindFirstValue(ClaimTypes.Name);
        var accountantId = User.FindFirstValue(ClaimTypes.NameIdentifier);

        try
        {
            var result = await _service.UnblockUserAsync(id, accountantName, accountantId);
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