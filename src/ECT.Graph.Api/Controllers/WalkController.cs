using ECT.Graph.Api.Graph.Walker;
using Microsoft.AspNetCore.Mvc;

namespace ECT.Graph.Api.Controllers;

/// <summary>
/// Triggers graph traversal and rollup over the ECT parameter topology.
///
/// Two walk modes:
///   Scenario walk   — baseline traversal using USES edge base values only
///   Configuration walk — merges OVERRIDES onto base values before traversal
///
/// The walker follows CONTRIBUTES_TO edges from leaves up to the root,
/// applying the rollup operator on each edge. Nodes not relevant to the
/// scenario's solve-for mode return N/A and are excluded from rollup.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WalkController : ControllerBase
{
    private readonly IGraphWalker _walker;
    private readonly ILogger<WalkController> _logger;

    public WalkController(IGraphWalker walker, ILogger<WalkController> logger)
    {
        _walker = walker;
        _logger = logger;
    }

    /// <summary>
    /// Walks the parameter topology for a scenario using base values only.
    /// No configuration overrides applied — this is the baseline rollup.
    /// </summary>
    /// <param name="scenarioNodeId">Graph Id of the ScenarioNode.</param>
    [HttpGet("scenario/{scenarioNodeId}")]
    [ProducesResponseType(typeof(WalkResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> WalkScenario(string scenarioNodeId)
    {
        try
        {
            var result = await _walker.WalkScenarioAsync(scenarioNodeId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Walk failed for scenario {ScenarioNodeId} — topology or USES edge missing", scenarioNodeId);
            return NotFound(new { error = ex.Message, scenarioNodeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error walking scenario {ScenarioNodeId}", scenarioNodeId);
            return StatusCode(500, new { error = "Walk failed unexpectedly.", detail = ex.Message });
        }
    }

    /// <summary>
    /// Walks the parameter topology for a configuration run.
    /// Base values from the USES edge are merged with OVERRIDES for the configuration
    /// before traversal — overrides win.
    /// </summary>
    /// <param name="scenarioNodeId">Graph Id of the ScenarioNode.</param>
    /// <param name="configurationNodeId">Graph Id of the ConfigurationNode.</param>
    [HttpGet("scenario/{scenarioNodeId}/configuration/{configurationNodeId}")]
    [ProducesResponseType(typeof(WalkResult), 200)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public async Task<IActionResult> WalkConfiguration(string scenarioNodeId, string configurationNodeId)
    {
        try
        {
            var result = await _walker.WalkConfigurationAsync(scenarioNodeId, configurationNodeId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex,
                "Walk failed for scenario {ScenarioNodeId} / configuration {ConfigurationNodeId}",
                scenarioNodeId, configurationNodeId);
            return NotFound(new { error = ex.Message, scenarioNodeId, configurationNodeId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected error walking configuration {ConfigurationNodeId}", configurationNodeId);
            return StatusCode(500, new { error = "Walk failed unexpectedly.", detail = ex.Message });
        }
    }
}
