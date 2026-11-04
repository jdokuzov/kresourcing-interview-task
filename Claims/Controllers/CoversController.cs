using Claims.Contracts;
using Claims.Domain;
using Claims.Services;
using Microsoft.AspNetCore.Mvc;

namespace Claims.Controllers;

/// <summary>Create, read and delete insurance covers, and quote premiums.</summary>
[ApiController]
[Route("[controller]")]
public class CoversController(ICoversService coversService) : ControllerBase
{
    /// <summary>Computes the premium for a prospective cover without storing anything.</summary>
    [HttpPost("compute")]
    public decimal ComputePremium(DateTime startDate, DateTime endDate, CoverType coverType) =>
        coversService.ComputePremium(startDate, endDate, coverType);

    /// <summary>Returns all covers.</summary>
    [HttpGet]
    public Task<IReadOnlyList<Cover>> GetAsync() => coversService.GetAllAsync();

    /// <summary>Returns a single cover.</summary>
    /// <response code="404">The cover does not exist.</response>
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Cover>> GetAsync(string id)
    {
        var cover = await coversService.GetByIdAsync(id);
        return cover is null ? NotFound() : cover;
    }

    /// <summary>Creates a cover; the premium is computed by the server.</summary>
    /// <response code="400">The start date is in the past, or the period is invalid or longer than a year.</response>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    public Task<Cover> CreateAsync(CreateCoverRequest request) => coversService.CreateAsync(request);

    /// <summary>Deletes a cover. Claims against it are left in place.</summary>
    /// <response code="404">The cover does not exist.</response>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteAsync(string id) =>
        await coversService.DeleteAsync(id) ? NoContent() : NotFound();
}
