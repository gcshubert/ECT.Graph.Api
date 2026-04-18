using ECT.Graph.Api.Domain.Nodes;
using ECT.Graph.Api.Domain.Edges;
using ECT.Graph.Api.Domain.Math;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Services;

// Request/Response DTOs for step creation

public class CreateStepRequest
{
    public string ScenarioId { get; set; } = string.Empty;
    public string StepName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ParentStepId { get; set; }
    public string? ParentRollupOperator { get; set; }
    public double? ParentWeight { get; set; }
    public int? ParentSortOrder { get; set; }
    public RollupOperator? RollupOperator { get; set; }
    public List<ParameterDefinition> Parameters { get; set; } = new();
}

public class ParameterDefinition
{
    public ParameterRole Role { get; set; }
    public ScientificValue Value { get; set; } = new ScientificValue(0, 0);
    public string? Provenance { get; set; }
    public string? Description { get; set; }
    public string? RollupOperator { get; set; }
    public double? Weight { get; set; }
    public int? SortOrder { get; set; }
}

public class StepCreationResult
{
    public ParameterNode AnchorNode { get; set; } = null!;
    public List<ParameterNode> LeafNodes { get; set; } = new();
    public List<ContributesToEdge> Edges { get; set; } = new();
}

public class StepResult
{
    public ParameterNode AnchorNode { get; set; } = null!;
    public List<ParameterNode> LeafNodes { get; set; } = new();
}

public interface IParameterNodeService
{
    Task<ParameterNode> CreateAsync(ParameterNode node);
    Task<ParameterNode?> GetByIdAsync(string id);
    Task<IEnumerable<ParameterNode>> GetAllAsync();
    Task<IEnumerable<ParameterNode>> GetByScenarioRootAsync(string rootNodeId);
    Task<ParameterNode?> UpdateAsync(ParameterNode node);
    Task<bool> DeleteAsync(string id);
    Task<bool> DeleteWithLeavesAsync(string anchorId);
    
    /// <summary>
    /// Deletes all parameter nodes for a scenario (clears the deck).
    /// </summary>
    Task<bool> DeleteAllForScenarioAsync(string scenarioId);
    
    // Step creation methods for hierarchical scenarios
    Task<StepCreationResult> CreateStepAsync(CreateStepRequest request);
    Task<IEnumerable<StepResult>> GetStepsAsync(string scenarioId);
}

public class ParameterNodeService : IParameterNodeService
{
    private readonly IGraphRepository _repo;
    private readonly IEdgeService _edgeService;

    public ParameterNodeService(IGraphRepository repo, IEdgeService edgeService) 
    {
        _repo = repo;
        _edgeService = edgeService;
    }

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
            isActive      = node.IsActive,
            coefficient   = node.Coefficient,
            exponent      = node.Exponent,
            provenance    = node.Provenance,
            externalScenarioId = node.ExternalScenarioId,
            // New parameter properties for step anchor nodes
            eCoefficient   = node.ECoefficient,
            eExponent      = node.EExponent,
            cCoefficient   = node.CCoefficient,
            cExponent      = node.CExponent,
            kCoefficient   = node.KCoefficient,
            kExponent      = node.KExponent,
            tCoefficient   = node.TCoefficient,
            tExponent      = node.TExponent,
            eProvenance    = node.EProvenance,
            cProvenance    = node.CProvenance,
            kProvenance    = node.KProvenance,
            tProvenance    = node.TProvenance
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

    public async Task<IEnumerable<ParameterNode>> GetByScenarioRootAsync(string rootNodeId)
    {
        await using var session = _repo.OpenSession();
        var cursor = await session.RunAsync(CypherQueries.GetParameterNodesByScenarioRoot, new { rootId = rootNodeId });
        var records = await cursor.ToListAsync();
        return records.Select(r => MapNode(r["node"].As<INode>()));
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
            isActive      = node.IsActive,
            coefficient   = node.Coefficient,
            exponent      = node.Exponent,
            provenance    = node.Provenance,
            externalScenarioId = node.ExternalScenarioId,
            // New parameter properties for step anchor nodes
            eCoefficient   = node.ECoefficient,
            eExponent      = node.EExponent,
            cCoefficient   = node.CCoefficient,
            cExponent      = node.CExponent,
            kCoefficient   = node.KCoefficient,
            kExponent      = node.KExponent,
            tCoefficient   = node.TCoefficient,
            tExponent      = node.TExponent,
            eProvenance    = node.EProvenance,
            cProvenance    = node.CProvenance,
            kProvenance    = node.KProvenance,
            tProvenance    = node.TProvenance
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

    /// <summary>
    /// Deletes all parameter nodes for a scenario (clears the deck).
    /// </summary>
    public async Task<bool> DeleteAllForScenarioAsync(string scenarioId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteAllParameterNodesForScenario, new { scenarioId });
        return true;
    }

    public async Task<bool> DeleteWithLeavesAsync(string anchorId)
    {
        await using var session = _repo.OpenSession();
        await session.RunAsync(CypherQueries.DeleteStepWithLeaves, new { anchorId });
        return true;
    }

    private static ParameterNode MapNode(INode n) => new()
    {
        Id = n.Properties.TryGetValue("id", out var id) ? id.As<string>() : string.Empty,
        Name = n.Properties.TryGetValue("name", out var name) ? name.As<string>() : string.Empty,
        Role = n.Properties.TryGetValue("role", out var role) && role is not null
                            ? Enum.Parse<ParameterRole>(role.As<string>(), ignoreCase: true) : ParameterRole.C,
        RollupOperator = n.Properties.TryGetValue("rollupOperator", out var op) && op is not null
                            ? Enum.Parse<RollupOperator>(op.As<string>(), ignoreCase: true) : null,
        Description = n.Properties.TryGetValue("description", out var d) ? d?.As<string>() : null,
        IsActive = n.Properties.TryGetValue("isActive", out var active) ? active.As<bool>() : true,
        Coefficient = n.Properties.TryGetValue("coefficient", out var coeff) ? coeff?.As<double>() : null,
        Exponent = n.Properties.TryGetValue("exponent", out var exp) ? exp?.As<double>() : null,
        Provenance = n.Properties.TryGetValue("provenance", out var prov) ? prov?.As<string>() : null,
        ExternalScenarioId = n.Properties.TryGetValue("externalScenarioId", out var externalScenarioId) ? externalScenarioId?.As<string>() : null,
        // New parameter properties for step anchor nodes
        ECoefficient = n.Properties.TryGetValue("eCoefficient", out var eCoeff) ? eCoeff?.As<double>() : null,
        EExponent = n.Properties.TryGetValue("eExponent", out var eExp) ? eExp?.As<double>() : null,
        CCoefficient = n.Properties.TryGetValue("cCoefficient", out var cCoeff) ? cCoeff?.As<double>() : null,
        CExponent = n.Properties.TryGetValue("cExponent", out var cExp) ? cExp?.As<double>() : null,
        KCoefficient = n.Properties.TryGetValue("kCoefficient", out var kCoeff) ? kCoeff?.As<double>() : null,
        KExponent = n.Properties.TryGetValue("kExponent", out var kExp) ? kExp?.As<double>() : null,
        TCoefficient = n.Properties.TryGetValue("tCoefficient", out var tCoeff) ? tCoeff?.As<double>() : null,
        TExponent = n.Properties.TryGetValue("tExponent", out var tExp) ? tExp?.As<double>() : null,
        EProvenance = n.Properties.TryGetValue("eProvenance", out var eProv) ? eProv?.As<string>() : null,
        CProvenance = n.Properties.TryGetValue("cProvenance", out var cProv) ? cProv?.As<string>() : null,
        KProvenance = n.Properties.TryGetValue("kProvenance", out var kProv) ? kProv?.As<string>() : null,
        TProvenance = n.Properties.TryGetValue("tProvenance", out var tProv) ? tProv?.As<string>() : null
    };

    // ── Step Creation Methods ─────────────────────────────────────────────────────

    /// <summary>
    /// Creates a step with parameters stored directly on the anchor node.
    /// No separate leaf nodes are created - parameters are stored as properties.
    /// </summary>
    public async Task<StepCreationResult> CreateStepAsync(CreateStepRequest request)
    {
        // 1. Create the anchor node with parameters stored as properties
        var anchorNode = new ParameterNode
        {
            Id = Guid.NewGuid().ToString(),
            Name = request.StepName,
            Role = ParameterRole.k, // Anchor nodes represent the step's k complexity
            RollupOperator = request.RollupOperator,
            Description = request.Description,
            IsActive = true,
            ExternalScenarioId = request.ScenarioId,
            
            // Store parameters directly on anchor node instead of separate leaf nodes
            ECoefficient = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.E)?.Value.Coefficient,
            EExponent = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.E)?.Value.Exponent,
            EProvenance = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.E)?.Provenance,
            
            CCoefficient = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.C)?.Value.Coefficient,
            CExponent = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.C)?.Value.Exponent,
            CProvenance = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.C)?.Provenance,
            
            KCoefficient = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.k)?.Value.Coefficient,
            KExponent = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.k)?.Value.Exponent,
            KProvenance = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.k)?.Provenance,
            
            TCoefficient = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.T)?.Value.Coefficient,
            TExponent = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.T)?.Value.Exponent,
            TProvenance = request.Parameters.FirstOrDefault(p => p.Role == ParameterRole.T)?.Provenance
        };

        var createdAnchor = await CreateAsync(anchorNode);

        // 2. If parent step is specified, create CONTRIBUTES_TO edge from anchor to parent
        var edges = new List<ContributesToEdge>();
        if (!string.IsNullOrEmpty(request.ParentStepId))
        {
            var parentEdge = new ContributesToEdge
            {
                Id = Guid.NewGuid().ToString(),
                FromParameterNodeId = createdAnchor.Id,
                ToParameterNodeId = request.ParentStepId,
                RollupOperator = request.ParentRollupOperator ?? "Sum",
                Weight = request.ParentWeight ?? 1.0,
                SortOrder = request.ParentSortOrder ?? 0
            };

            await _edgeService.CreateContributesToAsync(parentEdge);
            edges.Add(parentEdge);
        }

        return new StepCreationResult
        {
            AnchorNode = createdAnchor,
            LeafNodes = new List<ParameterNode>(), // No leaf nodes created
            Edges = edges
        };
    }

    /// <summary>
    /// Retrieves all steps for a scenario, including both anchor and leaf nodes.
    /// </summary>
    public async Task<IEnumerable<StepResult>> GetStepsAsync(string scenarioId)
    {
        await using var session = _repo.OpenSession();
        
        // Get all parameter nodes connected to this scenario via externalScenarioId
        var cursor = await session.RunAsync(@"
            MATCH (param:ParameterNode)
            WHERE param.externalScenarioId = $scenarioId
            RETURN param
            ORDER BY param.name
        ", new { scenarioId });

        var records = await cursor.ToListAsync();
        var nodes = records.Select(r => MapNode(r["param"].As<INode>())).ToList();

        // Group nodes by step (anchor nodes and their leaves)
        var steps = new List<StepResult>();
        var anchorNodes = nodes.Where(n => n.Coefficient == null && n.Exponent == null).ToList();
        
        foreach (var anchor in anchorNodes)
        {
            // Get leaf nodes that contribute to this anchor
            var leafCursor = await session.RunAsync(@"
                MATCH (leaf:ParameterNode)-[:CONTRIBUTES_TO]->(anchor:ParameterNode {id: $anchorId})
                RETURN leaf
                ORDER BY leaf.name
            ", new { anchorId = anchor.Id });

            var leafRecords = await leafCursor.ToListAsync();
            var leaves = leafRecords.Select(r => MapNode(r["leaf"].As<INode>())).ToList();

            steps.Add(new StepResult
            {
                AnchorNode = anchor,
                LeafNodes = leaves
            });
        }

        return steps;
    }
}
