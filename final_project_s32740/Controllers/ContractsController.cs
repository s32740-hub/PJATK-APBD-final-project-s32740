using final_project_s32740.Dtos;
using final_project_s32740.Exceptions;
using final_project_s32740.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_project_s32740.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ContractsController(IContractService contractService) : ControllerBase
{
    // GET /contracts/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        try
        {
            var result = await contractService.GetContractAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
    }

    // POST /contracts
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateContractDto dto)
    {
        try
        {
            var result = await contractService.CreateContractAsync(dto);
            return CreatedAtAction(nameof(Get), new { id = result.Id }, result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return Conflict(ex.Message); }
    }

    // DELETE /contracts/{id}
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await contractService.DeleteContractAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    // POST /contracts/payments
    [HttpPost("payments")]
    public async Task<IActionResult> AddPayment([FromBody] CreateContractPaymentDto dto)
    {
        try
        {
            var result = await contractService.AddPaymentAsync(dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }
}