using ECT.Graph.Api.Domain.Edges;
using ECT.Graph.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace ECT.Graph.Api.Controllers;

/// <summary>
/// Creates and manages edges in the ECT parameter topology graph.
///
/// Edge types:
///   CONTRIBUTES_TO — ParameterNode → ParameterNode (topology structure, rollup operator, weight)
///   USES           — ScenarioNode  → ParameterNode (establishes topology + carries base values)
///   BELONGS_TO     — ConfigurationNode → ScenarioNode
///   OVERRIDES      — ConfigurationNode → ParameterNode (carries override value for a run)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class EdgesController : ControllerBase
{
    private readonly IEdgeService _service;

    public EdgesController(IEdgeService service) => _service = service;

    // ── CONTRIBUTES_TO ────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a CONTRIBUTES_TO edge between two ParameterNodes.
    /// Defines the rollup topology — child contributes to parent with a given operator and weight.
    /// </summary>
    [HttpPost("contributes-to")]
    [ProducesResponseType(typeof(ContributesToEdge), 201)]
    public async Task<IActionResult> CreateContributesTo([FromBody] ContributesToEdge edge)
    {
        var created = await _service.CreateContributesToAsync(edge);
        return CreatedAtAction(nameof(CreateContributesTo), created);
    }

    /// <summary>
    /// Deletes a CONTRIBUTES_TO edge by its Id.
    /// </summary>
    [HttpDelete("contributes-to/{edgeId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteContributesTo(string edgeId)
    {
        await _service.DeleteContributesToAsync(edgeId);
        return NoContent();
    }

    // ── USES ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a USES edge from a ScenarioNode to its root ParameterNode.
    /// Establishes which topology the scenario operates over and carries base parameter values.
    /// Each scenario should have exactly one USES edge.
    /// </summary>
    [HttpPost("uses")]
    [ProducesResponseType(typeof(UsesEdge), 201)]
    public async Task<IActionResult> CreateUses([FromBody] UsesEdge edge)
    {
        var created = await _service.CreateUsesAsync(edge);
        return CreatedAtAction(nameof(CreateUses), created);
    }

    /// <summary>
    /// Upserts the USES edge for a scenario (deletes any existing edge and creates a new one).
    /// </summary>
    [HttpPut("uses/{scenarioNodeId}")]
    [ProducesResponseType(typeof(UsesEdge), 200)]
    public async Task<IActionResult> UpsertUses(string scenarioNodeId, [FromBody] UsesEdge edge)
    {
        edge.ScenarioNodeId = scenarioNodeId;
        var upserted = await _service.UpsertUsesAsync(edge);
        return Ok(upserted);
    }

    /// <summary>
    /// Gets the USES edge for a scenario.
    /// </summary>
    [HttpGet("uses/by-scenario/{scenarioNodeId}")]
    [ProducesResponseType(typeof(UsesEdge), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetUsesByScenario(string scenarioNodeId)
    {
        var uses = await _service.GetUsesEdgeForScenarioAsync(scenarioNodeId);
        return uses is null ? NotFound() : Ok(uses);
    }

    // ── BELONGS_TO ────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a BELONGS_TO edge from a ConfigurationNode to its ScenarioNode.
    /// </summary>
    [HttpPost("belongs-to")]
    [ProducesResponseType(typeof(BelongsToEdge), 201)]
    public async Task<IActionResult> CreateBelongsTo([FromBody] BelongsToEdge edge)
    {
        var created = await _service.CreateBelongsToAsync(edge);
        return CreatedAtAction(nameof(CreateBelongsTo), created);
    }

    // ── CONTRIBUTES_TO ─────────────────────────────────────────────────────────────
    /// <summary>
    /// Returns all CONTRIBUTES_TO edges — child/parent ID pairs with edge properties.
    /// Used by ECT.ACC.Api to reconstruct the parameter hierarchy tree.
    /// </summary>
    [HttpGet("contributes-to")]
    [ProducesResponseType(typeof(IEnumerable<ContributesToEdgeSummary>), 200)]
    public async Task<IActionResult> GetAllContributesTo()
    {
        var edges = await _service.GetAllContributesToAsync();
        return Ok(edges);
    }

    // ── OVERRIDES ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an OVERRIDES edge from a ConfigurationNode to a ParameterNode.
    /// Carries the override value that replaces the scenario base value for this parameter
    /// during a configuration run.
    /// </summary>
    [HttpPost("overrides")]
    [ProducesResponseType(typeof(OverridesEdge), 201)]
    public async Task<IActionResult> CreateOverrides([FromBody] OverridesEdge edge)
    {
        var created = await _service.CreateOverridesAsync(edge);
        return CreatedAtAction(nameof(CreateOverrides), created);
    }

    /// <summary>
    /// Returns all OVERRIDES edges for a given ConfigurationNode.
    /// Useful for inspecting which parameters a configuration run overrides before walking.
    /// </summary>
    [HttpGet("overrides/by-configuration/{configurationNodeId}")]
    [ProducesResponseType(typeof(IEnumerable<object>), 200)]
    public async Task<IActionResult> GetOverridesForConfiguration(string configurationNodeId)
    {
        var overrides = await _service.GetOverridesForConfigurationAsync(configurationNodeId);
        var result = overrides.Select(o => new
        {
            ParameterNodeId = o.ParameterNodeId,
            OverrideValue   = o.OverrideValue
        });
        return Ok(result);
    }

    /// <summary>
    /// Deletes an OVERRIDES edge by its Id.
    /// </summary>
    [HttpDelete("overrides/{edgeId}")]
    [ProducesResponseType(204)]
    public async Task<IActionResult> DeleteOverrides(string edgeId)
    {
        await _service.DeleteOverridesAsync(edgeId);
        return NoContent();
    }
}
