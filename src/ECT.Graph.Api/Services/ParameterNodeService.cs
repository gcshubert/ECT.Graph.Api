using ECT.Graph.Api.Domain.Nodes;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Services;

public interface IParameterNodeService
{
    Task<ParameterNode> CreateAsync(ParameterNode node);
    Task<ParameterNode?> GetByIdAsync(string id);
    Task<IEnumerable<ParameterNode>> GetAllAsync();
    Task<ParameterNode?> UpdateAsync(ParameterNode node);
    Task<bool> DeleteAsync(string id);
}

public class ParameterNodeService : IParameterNodeService
{
    private readonly IGraphRepository _repo;

    public ParameterNodeService(IGraphRepository repo) => _repo = repo;

    public async Task<ParameterNode> CreateAsync(ParameterNode node)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.CreateParameterNode, new
        {
            id            = node.Id,
            name          = node.Name,
            role          = node.Role.ToString(),
            rollupOperator= node.RollupOperator?.ToString(),
            description   = node.Description,
            isActive      = node.IsActive
        });
        var record = await cursor.SingleAsync();
        return MapNode(record["n"].As<INode>());
    }

    public async Task<ParameterNode?> GetByIdAsync(string id)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetParameterNodeById, new { id });
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? null : MapNode(records[0]["n"].As<INode>());
    }

    public async Task<IEnumerable<ParameterNode>> GetAllAsync()
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetAllParameterNodes);
        var records = await cursor.ToListAsync();
        return records.Select(r => MapNode(r["n"].As<INode>()));
    }

    public async Task<ParameterNode?> UpdateAsync(ParameterNode node)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.UpdateParameterNode, new
        {
            id            = node.Id,
            name          = node.Name,
            role          = node.Role.ToString(),
            rollupOperator= node.RollupOperator?.ToString(),
            description   = node.Description,
            isActive      = node.IsActive
        });
        var records = await cursor.ToListAsync();
        return records.Count == 0 ? null : MapNode(records[0]["n"].As<INode>());
    }

    public async Task<bool> DeleteAsync(string id)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteParameterNode, new { id });
        return true;
    }

    private static ParameterNode MapNode(INode n) => new()
    {
        Id             = n["id"].As<string>(),
        Name           = n["name"].As<string>(),
        Role           = Enum.Parse<ParameterRole>(n["role"].As<string>()),
        RollupOperator = n.Properties.TryGetValue("rollupOperator", out var op) && op is not null
                            ? Enum.Parse<RollupOperator>(op.As<string>()) : null,
        Description    = n.Properties.TryGetValue("description", out var d) ? d?.As<string>() : null,
        IsActive       = n["isActive"].As<bool>()
    };
}
