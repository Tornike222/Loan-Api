using LoansApi.Domain.Database;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace LoansApi.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AccountantController : ControllerBase
{
    private readonly LoanDbContext _ctx;
    
    public AccountantController(LoanDbContext ctx)
    {
        _ctx = ctx;
    }

    [HttpPost("blockUser/{id}")]
    [Authorize(Roles = "Accountant")]
    public async Task<IActionResult> BlockUser(int id)
    {
        var user = await _ctx.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");
        
        if (user.IsBlocked) 
            return StatusCode(403, "User is already blocked.");

        user.IsBlocked = true;

        await _ctx.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.IsBlocked,
        });
    }
    
    [HttpPost("unblockUser/{id}")]
    [Authorize(Roles = "Accountant")]
    public async Task<IActionResult> UnblockUser(int id)
    {
        var user = await _ctx.Users.FindAsync(id);
        if (user == null)
            return NotFound("User not found.");

        if (!user.IsBlocked)
            return StatusCode(403, "User is not blocked.");

        user.IsBlocked = false;

        await _ctx.SaveChangesAsync();

        return Ok(new
        {
            user.Id,
            user.Username,
            user.IsBlocked,
        });
    }
}