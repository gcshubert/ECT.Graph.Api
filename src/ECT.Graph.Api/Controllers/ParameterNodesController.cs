using ECT.Graph.Api.Domain.Nodes;
using ECT.Graph.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECT.Graph.Api.Controllers;

/// <summary>
/// CRUD operations for ParameterNodes — the typed parameter vertices in the ECT topology graph.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ParameterNodesController : ControllerBase
{
    private readonly IParameterNodeService _service;

    public ParameterNodesController(IParameterNodeService service) => _service = service;

    /// <summary>Returns all ParameterNodes.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ParameterNode>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Returns a ParameterNode by Id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ParameterNode), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _service.GetByIdAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    /// <summary>Creates a new ParameterNode.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ParameterNode), 201)]
    public async Task<IActionResult> Create([FromBody] ParameterNode node)
    {
        var created = await _service.CreateAsync(node);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing ParameterNode.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ParameterNode), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] ParameterNode node)
    {
        node.Id = id;
        var updated = await _service.UpdateAsync(node);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a ParameterNode and all its edges (DETACH DELETE).</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
