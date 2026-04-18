using ECT.Graph.Api.Domain.Edges;
using ECT.Graph.Api.Domain.Math;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Services;

public interface IEdgeService
{
    Task<IEnumerable<ContributesToEdgeSummary>> GetAllContributesToAsync();
    Task<IEnumerable<ContributesToEdgeSummary>> GetContributesToByScenarioRootAsync(string rootNodeId);
    Task<IEnumerable<ContributesToEdgeSummary>> GetContributesToByScenarioIdAsync(string scenarioId);
    Task<ContributesToEdge> CreateContributesToAsync(ContributesToEdge edge);
    Task<int> GetMaxSortOrderForParentAsync(string parentId);
    Task<bool> DeleteContributesToAsync(string edgeId);
    Task<UsesEdge> CreateUsesAsync(UsesEdge edge);
    Task<UsesEdge?> GetUsesEdgeForScenarioAsync(string scenarioNodeId);
    Task DeleteUsesEdgesForScenarioAsync(string scenarioNodeId);
    Task<UsesEdge> UpsertUsesAsync(UsesEdge edge);
    Task<BelongsToEdge> CreateBelongsToAsync(BelongsToEdge edge);
    Task<OverridesEdge> CreateOverridesAsync(OverridesEdge edge);
    Task<IEnumerable<(string ParameterNodeId, ScientificValue OverrideValue)>> GetOverridesForConfigurationAsync(string configurationNodeId);
    Task<bool> DeleteOverridesAsync(string edgeId);
    Task<IEnumerable<DependsOnEdgeSummary>> GetAllDependsOnAsync();
    Task<DependsOnEdge> CreateDependsOnAsync(DependsOnEdge edge);
    Task<DependsOnEdge?> GetDependsOnByIdAsync(string edgeId);
    Task<bool> DeleteDependsOnAsync(string edgeId);
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
            Id             = r["id"].As<string>(),
            ChildId        = r["childId"].As<string>(),
            ParentId       = r["parentId"].As<string>(),
            Weight         = r["weight"].As<double?>(null) ?? 1.0,
            RollupOperator = r["rollupOperator"]?.As<string>(),
            SortOrder      = r["sortOrder"].As<int?>(null) ?? 0
        });
    }

    public async Task<IEnumerable<ContributesToEdgeSummary>> GetContributesToByScenarioRootAsync(string rootNodeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetContributesToEdgesByScenarioRoot, new { rootId = rootNodeId });
        var records = await cursor.ToListAsync();
        return records.Select(r => new ContributesToEdgeSummary
        {
            Id             = r["id"].As<string>(),
            ChildId        = r["childId"].As<string>(),
            ParentId       = r["parentId"].As<string>(),
            Weight         = r["weight"].As<double?>(null) ?? 1.0,
            RollupOperator = r["rollupOperator"]?.As<string>(),
            SortOrder      = r["sortOrder"].As<int?>(null) ?? 0
        });
    }

    public async Task<IEnumerable<ContributesToEdgeSummary>> GetContributesToByScenarioIdAsync(string scenarioId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetContributesToEdgesByScenarioId, new { scenarioId });
        var records = await cursor.ToListAsync();
        return records.Select(r => new ContributesToEdgeSummary
        {
            Id             = r["id"].As<string>(),
            ChildId        = r["childId"].As<string>(),
            ParentId       = r["parentId"].As<string>(),
            Weight         = r["weight"].As<double?>(null) ?? 1.0,
            RollupOperator = r["rollupOperator"]?.As<string>(),
            SortOrder      = r["sortOrder"].As<int?>(null) ?? 0
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
            weight         = edge.Weight,
            sortOrder = edge.SortOrder
        });
        return edge;
    }

    public async Task<int> GetMaxSortOrderForParentAsync(string parentId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(
            CypherQueries.GetMaxSortOrderForParent, new { parentId });
        var records = await cursor.ToListAsync();
        return records.Count > 0 ? records[0]["maxSortOrder"].As<int?>(null) ?? 0 : 0;
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
                BaseParameterValues = new Dictionary<string, ScientificValue>()
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
    private Dictionary<string, ScientificValue> SafeDeserialize(IRelationship r)
    {
        if (!r.Properties.TryGetValue("baseParameterValues", out var json)) return new();

        try
        {
            var options = new System.Text.Json.JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var raw = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, ScientificValueRaw>>(
                json.As<string>(), options);

            if (raw != null)
                return raw.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new ScientificValue(kvp.Value.Coefficient, kvp.Value.Exponent));
        }
        catch { }

        return new Dictionary<string, ScientificValue>();
    }

    private sealed record ScientificValueRaw
    {
        public double Coefficient { get; init; }
        public double Exponent { get; init; }
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
        var valueJson = System.Text.Json.JsonSerializer.Serialize(edge.OverrideValue);
        await session.RunAsync(CypherQueries.CreateOverridesEdge, new
        {
            configurationNodeId = edge.ConfigurationNodeId,
            parameterNodeId     = edge.ParameterNodeId,
            id                  = edge.Id,
            overrideValue       = valueJson
        });
        return edge;
    }

    public async Task<IEnumerable<(string ParameterNodeId, ScientificValue OverrideValue)>> GetOverridesForConfigurationAsync(
        string configurationNodeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetOverridesForConfiguration, new { configurationNodeId });
        var records = await cursor.ToListAsync();
        return records.Select(r => {
            var paramId = r["p"].As<INode>()["id"].As<string>();
            var valueJson = r["r"].As<IRelationship>()["overrideValue"].As<string>();
            ScientificValue value;
            try
            {
                value = System.Text.Json.JsonSerializer.Deserialize<ScientificValue>(valueJson) ?? new ScientificValue(0, 0);
            }
            catch
            {
                // Fallback for old double format
                var oldValue = double.Parse(valueJson);
                value = new ScientificValue(oldValue);
            }
            return (paramId, value);
        });
    }

    public async Task<bool> DeleteOverridesAsync(string edgeId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteOverridesEdge, new { id = edgeId });
        return true;
    }

    // ── DEPENDS_ON ─────────────────────────────────────────────────────────────

    public async Task<IEnumerable<DependsOnEdgeSummary>> GetAllDependsOnAsync()
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetAllDependsOnEdges);
        var records = await cursor.ToListAsync();
        return records.Select(r => new DependsOnEdgeSummary
        {
            Id = r["id"].As<string>(),
            FromId = r["fromId"].As<string>(),
            ToId = r["toId"].As<string>(),
            Description = r["description"]?.As<string>(),
            Strength = r["strength"].As<double?>(null) ?? 1.0,
            SortOrder = r["sortOrder"].As<int?>(null) ?? 0
        });
    }

    public async Task<DependsOnEdge> CreateDependsOnAsync(DependsOnEdge edge)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.CreateDependsOnEdge, new
        {
            fromId = edge.FromParameterNodeId,
            toId = edge.ToParameterNodeId,
            id = edge.Id,
            description = edge.Description,
            strength = edge.Strength,
            sortOrder = edge.SortOrder
        });
        return edge;
    }

    public async Task<DependsOnEdge?> GetDependsOnByIdAsync(string edgeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetDependsOnEdgeById, new { id = edgeId });
        var records = await cursor.ToListAsync();
        
        if (records.Count == 0) return null;
        
        var r = records[0]["r"].As<IRelationship>();
        
        // Use a single query to get the relationship with both nodes and their IDs
        await using var session2 = _repo.OpenSession();
        var fullCursor = await session2.RunAsync(@" 
            MATCH (from)-[r]->(to)
            WHERE elementId(r) = $elementId
            RETURN from.id AS fromId, to.id AS toId, r
        ", new { elementId = r.ElementId });
        
        var fullRecord = await fullCursor.ToListAsync();
        if (fullRecord.Count == 0) return null;
        
        var record = fullRecord[0];
        return new DependsOnEdge
        {
            Id = r.Properties.TryGetValue("id", out var id) ? id.As<string>() : edgeId,
            FromParameterNodeId = record["fromId"].As<string>(),
            ToParameterNodeId = record["toId"].As<string>(),
            Description = r.Properties.TryGetValue("description", out var desc) ? desc?.As<string>() : null,
            Strength = r.Properties.TryGetValue("strength", out var strength) ? strength.As<double>() : 1.0,
            SortOrder = r.Properties.TryGetValue("sortOrder", out var sortOrder) ? sortOrder.As<int>() : 0
        };
    }

    public async Task<bool> DeleteDependsOnAsync(string edgeId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteDependsOnEdge, new { id = edgeId });
        return true;
    }
}
