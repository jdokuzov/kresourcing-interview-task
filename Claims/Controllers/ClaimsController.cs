using Claims.Contracts;
using Claims.Domain;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

/// <summary>Create, read and delete insurance claims.</summary>
[ApiController]
[Route("[controller]")]
public class ClaimsController(IClaimsService claimsService) : ControllerBase
{
    /// <summary>Returns all claims.</summary>
    [HttpGet]
    public Task<IReadOnlyList<Claim>> GetAsync() => claimsService.GetAllAsync();

    /// <summary>Returns a single claim.</summary>
    /// <response code="404">The claim does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Claim>> GetAsync(string id)
    {
        var claim = await claimsService.GetByIdAsync(id);
        return claim is null ? NotFound() : claim;
    }

    /// <summary>Files a new claim.</summary>
    /// <response code="400">The damage cost exceeds the limit, or the date is outside the cover period.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<Claim> CreateAsync(CreateClaimRequest request) => claimsService.CreateAsync(request);

    /// <summary>Deletes a claim.</summary>
    /// <response code="404">The claim does not exist.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id) =>
        await claimsService.DeleteAsync(id) ? NoContent() : NotFound();
}
