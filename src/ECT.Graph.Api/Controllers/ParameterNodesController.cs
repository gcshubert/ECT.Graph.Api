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

    [HttpDelete("{id}/with-leaves")]
    public async Task<IActionResult> DeleteWithLeaves(string id)
    {
        await _service.DeleteWithLeavesAsync(id);
        return NoContent();
    }

    /// <summary>Deletes all ParameterNodes for a scenario (clears the deck).</summary>
    [HttpDelete("scenario/{scenarioId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteAllForScenario(string scenarioId)
    {
        await _service.DeleteAllForScenarioAsync(scenarioId);
        return NoContent();
    }

    // ── Step Creation Endpoints ─────────────────────────────────────────────

    /// <summary>
    /// Creates a step with anchor and leaf nodes, automatically wiring CONTRIBUTES_TO edges.
    /// </summary>
    [HttpPost("steps")]
    [ProducesResponseType(typeof(StepCreationResult), 201)]
    public async Task<IActionResult> CreateStep([FromBody] CreateStepRequest request)
    {
        var result = await _service.CreateStepAsync(request);
        return CreatedAtAction(nameof(CreateStep), result);
    }

    /// <summary>
    /// Gets all ParameterNodes for a scenario, returning a flat list of nodes.
    /// </summary>
    [HttpGet("by-scenario/{scenarioId}")]
    [ProducesResponseType(typeof(IEnumerable<ParameterNode>), 200)]
    public async Task<IActionResult> GetStepsByScenario(string scenarioId)
    {
        // Return flat node list - GetStepsAsync returns StepResult objects, wrong shape
        var allNodes = await _service.GetAllAsync();
        var scenarioNodes = allNodes.Where(n => n.ExternalScenarioId == scenarioId);
        return Ok(scenarioNodes);
    }

    /// <summary>
    /// Returns all ParameterNodes in the subtree rooted at the given node.
    /// Scoped to a single scenario — pass the scenario root node id (e.g. "scenario-6-root").
    /// </summary>
    [HttpGet("by-scenario-root/{rootNodeId}")]
    [ProducesResponseType(typeof(IEnumerable<ParameterNode>), 200)]
    public async Task<IActionResult> GetByScenarioRoot(string rootNodeId)
    {
        var nodes = await _service.GetByScenarioRootAsync(rootNodeId);
        return Ok(nodes);
    }

}
