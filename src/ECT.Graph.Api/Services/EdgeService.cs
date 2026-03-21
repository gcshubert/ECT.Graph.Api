using ECT.Graph.Api.Domain.Edges;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Services;

public interface IEdgeService
{
    Task<IEnumerable<ContributesToEdgeSummary>> GetAllContributesToAsync();
    Task<ContributesToEdge> CreateContributesToAsync(ContributesToEdge edge);
    Task<bool> DeleteContributesToAsync(string edgeId);

    Task<UsesEdge> CreateUsesAsync(UsesEdge edge);
    Task<UsesEdge?> GetUsesEdgeForScenarioAsync(string scenarioNodeId);
    Task DeleteUsesEdgesForScenarioAsync(string scenarioNodeId);
    Task<UsesEdge> UpsertUsesAsync(UsesEdge edge);

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

    public async Task<IEnumerable<ContributesToEdgeSummary>> GetAllContributesToAsync()
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetAllContributesToEdges);
        var records = await cursor.ToListAsync();
        return records.Select(r => new ContributesToEdgeSummary
        {
            ChildId = r["childId"].As<string>(),
            ParentId = r["parentId"].As<string>(),
            Weight = r["weight"].As<double>(),
            RollupOperator = r["rollupOperator"]?.As<string>()
        });
    }

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

    public async Task<UsesEdge?> GetUsesEdgeForScenarioAsync(string scenarioNodeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetUsesEdgeForScenario, new { scenarioNodeId });
        var records = await cursor.ToListAsync();

        // 1. SELF-HEALING: If missing, create it on the fly
        if (records.Count == 0)
        {
            var newEdge = new UsesEdge
            {
                Id = $"edge-{scenarioNodeId}",
                ScenarioNodeId = scenarioNodeId,
                RootParameterNodeId = $"root-{scenarioNodeId}",
                BaseParameterValues = new Dictionary<string, double>()
            };
            return await CreateUsesAsync(newEdge);
        }

        var r = records[0]["r"].As<IRelationship>();

        // 2. DEFENSIVE PARSING: Use TryGetValue to avoid KeyNotFoundException
        // This handles cases where the property names in Neo4j don't match exactly
        return new UsesEdge
        {
            Id = r.Properties.TryGetValue("id", out var id) ? id.As<string>() : $"edge-{scenarioNodeId}",
            ScenarioNodeId = r.Properties.TryGetValue("scenarioNodeId", out var sId) ? sId.As<string>() : scenarioNodeId,
            RootParameterNodeId = r.Properties.TryGetValue("rootParameterNodeId", out var rId) ? rId.As<string>() : $"root-{scenarioNodeId}",
            BaseParameterValues = SafeDeserialize(r)
        };
    }

    // Helper to keep the main method clean
    private Dictionary<string, double> SafeDeserialize(IRelationship r)
    {
        if (!r.Properties.TryGetValue("baseParameterValues", out var json)) return new();

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(json.As<string>())
                   ?? new Dictionary<string, double>();
        }
        catch
        {
            return new Dictionary<string, double>();
        }
    }

    public async Task DeleteUsesEdgesForScenarioAsync(string scenarioNodeId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteUsesEdgesForScenario, new { scenarioNodeId });
    }

    public async Task<UsesEdge> UpsertUsesAsync(UsesEdge edge)
    {
        await DeleteUsesEdgesForScenarioAsync(edge.ScenarioNodeId);
        return await CreateUsesAsync(edge);
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
