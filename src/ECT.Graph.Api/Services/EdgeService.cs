using ECT.Graph.Api.Domain.Edges;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Services;

public interface IEdgeService
{
    Task<ContributesToEdge> CreateContributesToAsync(ContributesToEdge edge);
    Task<bool> DeleteContributesToAsync(string edgeId);

    Task<UsesEdge> CreateUsesAsync(UsesEdge edge);

    Task<BelongsToEdge> CreateBelongsToAsync(BelongsToEdge edge);

    Task<OverridesEdge> CreateOverridesAsync(OverridesEdge edge);
    Task<IEnumerable<(string ParameterNodeId, double OverrideValue)>> GetOverridesForConfigurationAsync(string configurationNodeId);
    Task<bool> DeleteOverridesAsync(string edgeId);
}

public class EdgeService : IEdgeService
{
    private readonly IGraphRepository _repo;

    public EdgeService(IGraphRepository repo) => _repo = repo;

    // ── CONTRIBUTES_TO ────────────────────────────────────────────────────────

    public async Task<ContributesToEdge> CreateContributesToAsync(ContributesToEdge edge)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.CreateContributesToEdge, new
        {
            fromId         = edge.FromParameterNodeId,
            toId           = edge.ToParameterNodeId,
            id             = edge.Id,
            rollupOperator = edge.RollupOperator,
            weight         = edge.Weight
        });
        return edge;
    }

    public async Task<bool> DeleteContributesToAsync(string edgeId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteContributesToEdge, new { id = edgeId });
        return true;
    }

    // ── USES ──────────────────────────────────────────────────────────────────

    public async Task<UsesEdge> CreateUsesAsync(UsesEdge edge)
    {
        await using var session = _repo.OpenSession();
        // Neo4j cannot store Dictionary as a property — serialise to JSON string
        var valuesJson = System.Text.Json.JsonSerializer.Serialize(edge.BaseParameterValues);
        await session.RunAsync(CypherQueries.CreateUsesEdge, new
        {
            scenarioNodeId        = edge.ScenarioNodeId,
            rootParameterNodeId   = edge.RootParameterNodeId,
            id                    = edge.Id,
            baseParameterValues   = valuesJson
        });
        return edge;
    }

    // ── BELONGS_TO ────────────────────────────────────────────────────────────

    public async Task<BelongsToEdge> CreateBelongsToAsync(BelongsToEdge edge)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.CreateBelongsToEdge, new
        {
            configurationNodeId = edge.ConfigurationNodeId,
            scenarioNodeId      = edge.ScenarioNodeId,
            id                  = edge.Id
        });
        return edge;
    }

    // ── OVERRIDES ─────────────────────────────────────────────────────────────

    public async Task<OverridesEdge> CreateOverridesAsync(OverridesEdge edge)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.CreateOverridesEdge, new
        {
            configurationNodeId = edge.ConfigurationNodeId,
            parameterNodeId     = edge.ParameterNodeId,
            id                  = edge.Id,
            overrideValue       = edge.OverrideValue
        });
        return edge;
    }

    public async Task<IEnumerable<(string ParameterNodeId, double OverrideValue)>> GetOverridesForConfigurationAsync(
        string configurationNodeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetOverridesForConfiguration, new { configurationNodeId });
        var records = await cursor.ToListAsync();
        return records.Select(r => (
            r["p"].As<INode>()["id"].As<string>(),
            r["r"].As<IRelationship>()["overrideValue"].As<double>()
        ));
    }

    public async Task<bool> DeleteOverridesAsync(string edgeId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteOverridesEdge, new { id = edgeId });
        return true;
    }
}
