using final_project_s32740.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_project_s32740.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class RevenueController(IRevenueService revenueService) : ControllerBase
{
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrent(
        [FromQuery] int? softwareId = null,
        [FromQuery] string currency = "PLN")
    {
        try
        {
            var result = await revenueService.GetCurrentRevenueAsync(softwareId, currency);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
    [HttpGet("predicted")]
    public async Task<IActionResult> GetPredicted(
        [FromQuery] int? softwareId = null,
        [FromQuery] string currency = "PLN")
    {
        try
        {
            var result = await revenueService.GetPredictedRevenueAsync(softwareId, currency);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}