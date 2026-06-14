using final_project_s32740.DTOs;
using final_project_s32740.Exceptions;
using final_project_s32740.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_project_s32740.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class SubscriptionsController(ISubscriptionService subscriptionService) : ControllerBase
{
    // POST /subscriptions
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateSubscriptionDto dto)
    {
        try
        {
            var result = await subscriptionService.CreateSubscriptionAsync(dto);
            return CreatedAtAction(nameof(Create), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return Conflict(ex.Message); }
    }

    // POST /subscriptions/renew
    [HttpPost("renew")]
    public async Task<IActionResult> PayRenewal([FromBody] SubscriptionRenewalPaymentDto dto)
    {
        try
        {
            var result = await subscriptionService.PayRenewalAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }
}