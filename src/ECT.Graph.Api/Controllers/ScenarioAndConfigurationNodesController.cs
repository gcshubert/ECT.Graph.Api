using ECT.Graph.Api.Domain.Nodes;
using ECT.Graph.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECT.Graph.Api.Controllers;

// ── ScenarioNodes ─────────────────────────────────────────────────────────────

/// <summary>
/// CRUD operations for ScenarioNodes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ScenarioNodesController : ControllerBase
{
    private readonly IScenarioNodeService _service;

    public ScenarioNodesController(IScenarioNodeService service) => _service = service;

    /// <summary>Returns all ScenarioNodes.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ScenarioNode>), 200)]
    public async Task<IActionResult> GetAll()
        => Ok(await _service.GetAllAsync());

    /// <summary>Returns a ScenarioNode by its graph Id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ScenarioNode), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _service.GetByIdAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    /// <summary>
    /// Returns a ScenarioNode by the external scenario Id from ECT.ACC.Api (SQL Server).
    /// This is the primary lookup path when ECT.ACC.Api delegates to this service.
    /// </summary>
    [HttpGet("by-external-id/{externalScenarioId}")]
    [ProducesResponseType(typeof(ScenarioNode), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByExternalId(string externalScenarioId)
    {
        var node = await _service.GetByExternalIdAsync(externalScenarioId);
        return node is null ? NotFound() : Ok(node);
    }

    /// <summary>Creates a new ScenarioNode.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ScenarioNode), 201)]
    public async Task<IActionResult> Create([FromBody] ScenarioNode node)
    {
        var created = await _service.CreateAsync(node);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Updates an existing ScenarioNode.</summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ScenarioNode), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(string id, [FromBody] ScenarioNode node)
    {
        node.Id = id;
        var updated = await _service.UpdateAsync(node);
        return updated is null ? NotFound() : Ok(updated);
    }

    /// <summary>Deletes a ScenarioNode and all its edges.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

// ── ConfigurationNodes ────────────────────────────────────────────────────────

/// <summary>
/// CRUD operations for ConfigurationNodes.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ConfigurationNodesController : ControllerBase
{
    private readonly IConfigurationNodeService _service;

    public ConfigurationNodesController(IConfigurationNodeService service) => _service = service;

    /// <summary>Returns a ConfigurationNode by Id.</summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ConfigurationNode), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(string id)
    {
        var node = await _service.GetByIdAsync(id);
        return node is null ? NotFound() : Ok(node);
    }

    /// <summary>Returns all ConfigurationNodes belonging to a scenario.</summary>
    [HttpGet("by-scenario/{scenarioNodeId}")]
    [ProducesResponseType(typeof(IEnumerable<ConfigurationNode>), 200)]
    public async Task<IActionResult> GetByScenario(string scenarioNodeId)
        => Ok(await _service.GetByScenarioIdAsync(scenarioNodeId));

    /// <summary>Creates a new ConfigurationNode.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConfigurationNode), 201)]
    public async Task<IActionResult> Create([FromBody] ConfigurationNode node)
    {
        var created = await _service.CreateAsync(node);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Deletes a ConfigurationNode and all its edges.</summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}
