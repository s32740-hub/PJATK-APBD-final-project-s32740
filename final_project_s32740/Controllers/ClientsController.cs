using final_project_s32740.Dtos;
using final_project_s32740.Exceptions;
using final_project_s32740.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace final_project_s32740.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class ClientsController(IClientService clientService) : ControllerBase
{

    // POST /clients/individual
    [HttpPost("individual")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateIndividual([FromBody] CreateIndividualClientDto dto)
    {
        try
        {
            var result = await clientService.CreateIndividualClientAsync(dto);
            return CreatedAtAction(nameof(CreateIndividual), new { id = result.Id }, result);
        }
        catch (DomainException ex) { return Conflict(ex.Message); }
    }

    // PUT /clients/individual/{id}
    [HttpPut("individual/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateIndividual(int id, [FromBody] UpdateIndividualClientDto dto)
    {
        try
        {
            var result = await clientService.UpdateIndividualClientAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }

    // DELETE /clients/individual/{id}
    [HttpDelete("individual/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteIndividual(int id)
    {
        try
        {
            await clientService.DeleteIndividualClientAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }
    
    // POST /clients/corporate
    [HttpPost("corporate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateCorporate([FromBody] CreateCorporateClientDto dto)
    {
        try
        {
            var result = await clientService.CreateCorporateClientAsync(dto);
            return CreatedAtAction(nameof(CreateCorporate), new { id = result.Id }, result);
        }
        catch (DomainException ex) { return Conflict(ex.Message); }
    }

    // PUT /clients/corporate/{id}
    [HttpPut("corporate/{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCorporate(int id, [FromBody] UpdateCorporateClientDto dto)
    {
        try
        {
            var result = await clientService.UpdateCorporateClientAsync(id, dto);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(ex.Message); }
        catch (DomainException ex) { return BadRequest(ex.Message); }
    }
}