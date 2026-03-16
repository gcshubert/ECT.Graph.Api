using ECT.Graph.Api.Domain.Nodes;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Services;

// ── ScenarioNode service ──────────────────────────────────────────────────────

public interface IScenarioNodeService
{
    Task<ScenarioNode> CreateAsync(ScenarioNode node);
    Task<ScenarioNode?> GetByIdAsync(string id);
    Task<ScenarioNode?> GetByExternalIdAsync(string externalScenarioId);
    Task<IEnumerable<ScenarioNode>> GetAllAsync();
    Task<ScenarioNode?> UpdateAsync(ScenarioNode node);
    Task<bool> DeleteAsync(string id);
}

public class ScenarioNodeService : IScenarioNodeService
{
    private readonly IGraphRepository _repo;

    public ScenarioNodeService(IGraphRepository repo) => _repo = repo;

    public async Task<ScenarioNode> CreateAsync(ScenarioNode node)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.CreateScenarioNode, new
        {
            id                 = node.Id,
            name               = node.Name,
            solveForMode       = node.SolveForMode.ToString(),
            domain             = node.Domain,
            externalScenarioId = node.ExternalScenarioId,
            description        = node.Description
        });
        var record = await cursor.SingleAsync();
        return MapNode(record["n"].As<INode>());
    }

    public async Task<ScenarioNode?> GetByIdAsync(string id)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetScenarioNodeById, new { id });
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? null : MapNode(records[0]["n"].As<INode>());
    }

    public async Task<ScenarioNode?> GetByExternalIdAsync(string externalScenarioId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetScenarioNodeByExternalId, new { externalScenarioId });
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? null : MapNode(records[0]["n"].As<INode>());
    }

    public async Task<IEnumerable<ScenarioNode>> GetAllAsync()
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetAllScenarioNodes);
        var records = await cursor.ToListAsync();
        return records.Select(r => MapNode(r["n"].As<INode>()));
    }

    public async Task<ScenarioNode?> UpdateAsync(ScenarioNode node)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.UpdateScenarioNode, new
        {
            id                 = node.Id,
            name               = node.Name,
            solveForMode       = node.SolveForMode.ToString(),
            domain             = node.Domain,
            externalScenarioId = node.ExternalScenarioId,
            description        = node.Description
        });
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? null : MapNode(records[0]["n"].As<INode>());
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteScenarioNode, new { id });
        return true;
    }

    private static ScenarioNode MapNode(INode n) => new()
    {
        Id                 = n["id"].As<string>(),
        Name               = n["name"].As<string>(),
        SolveForMode       = Enum.Parse<SolveForMode>(n["solveForMode"].As<string>()),
        Domain             = n["domain"].As<string>(),
        ExternalScenarioId = n.Properties.TryGetValue("externalScenarioId", out var eid) ? eid?.As<string>() : null,
        Description        = n.Properties.TryGetValue("description", out var d) ? d?.As<string>() : null
    };
}

// ── ConfigurationNode service ─────────────────────────────────────────────────

public interface IConfigurationNodeService
{
    Task<ConfigurationNode> CreateAsync(ConfigurationNode node);
    Task<ConfigurationNode?> GetByIdAsync(string id);
    Task<IEnumerable<ConfigurationNode>> GetByScenarioIdAsync(string scenarioNodeId);
    Task<bool> DeleteAsync(string id);
}

public class ConfigurationNodeService : IConfigurationNodeService
{
    private readonly IGraphRepository _repo;

    public ConfigurationNodeService(IGraphRepository repo) => _repo = repo;

    public async Task<ConfigurationNode> CreateAsync(ConfigurationNode node)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.CreateConfigurationNode, new
        {
            id                      = node.Id,
            name                    = node.Name,
            scenarioNodeId          = node.ScenarioNodeId,
            externalConfigurationId = node.ExternalConfigurationId,
            description             = node.Description
        });
        var record = await cursor.SingleAsync();
        return MapNode(record["n"].As<INode>());
    }

    public async Task<ConfigurationNode?> GetByIdAsync(string id)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetConfigurationNodeById, new { id });
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? null : MapNode(records[0]["n"].As<INode>());
    }

    public async Task<IEnumerable<ConfigurationNode>> GetByScenarioIdAsync(string scenarioNodeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetConfigurationsByScenarioId, new { scenarioNodeId });
        var records = await cursor.ToListAsync();
        return records.Select(r => MapNode(r["n"].As<INode>()));
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteConfigurationNode, new { id });
        return true;
    }

    private static ConfigurationNode MapNode(INode n) => new()
    {
        Id                      = n["id"].As<string>(),
        Name                    = n["name"].As<string>(),
        ScenarioNodeId          = n["scenarioNodeId"].As<string>(),
        ExternalConfigurationId = n.Properties.TryGetValue("externalConfigurationId", out var eid) ? eid?.As<string>() : null,
        Description             = n.Properties.TryGetValue("description", out var d) ? d?.As<string>() : null
    };
}
