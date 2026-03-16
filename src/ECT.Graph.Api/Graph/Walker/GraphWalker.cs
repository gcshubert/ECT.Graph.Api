using ECT.Graph.Api.Domain.Nodes;
using ECT.Graph.Api.Graph.Queries;
using ECT.Graph.Api.Infrastructure;
using Neo4j.Driver;

namespace ECT.Graph.Api.Graph.Walker;

// ── Result types ──────────────────────────────────────────────────────────────

/// <summary>
/// Result of a single node's evaluation during traversal.
/// </summary>
public record NodeResult(
    string NodeId,
    string Name,
    ParameterRole Role,
    double? EffectiveValue,   // null = N/A (node not relevant to this solve-for mode)
    double Weight,
    string RollupOperator,
    bool IsLeaf,
    List<NodeResult> Children
);

/// <summary>
/// Top-level traversal result for a scenario/configuration run.
/// </summary>
public record WalkResult(
    string ScenarioNodeId,
    string? ConfigurationNodeId,
    SolveForMode SolveForMode,
    NodeResult RootResult,
    double? RollupValue,      // null = no active nodes contributed to root
    DateTimeOffset ComputedAt
);

// ── Walker interface and implementation ───────────────────────────────────────

public interface IGraphWalker
{
    /// <summary>
    /// Walk the parameter topology for a scenario, applying base values from the USES edge.
    /// No configuration overrides applied — baseline traversal.
    /// </summary>
    Task<WalkResult> WalkScenarioAsync(string scenarioNodeId);

    /// <summary>
    /// Walk the parameter topology for a configuration run.
    /// Base values from USES edge are merged with OVERRIDES edges for the configuration.
    /// </summary>
    Task<WalkResult> WalkConfigurationAsync(string scenarioNodeId, string configurationNodeId);
}

public class GraphWalker : IGraphWalker
{
    private readonly IGraphRepository _repo;
    private readonly ILogger<GraphWalker> _logger;

    public GraphWalker(IGraphRepository repo, ILogger<GraphWalker> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<WalkResult> WalkScenarioAsync(string scenarioNodeId)
    {
        return await WalkInternalAsync(scenarioNodeId, configurationNodeId: null);
    }

    public async Task<WalkResult> WalkConfigurationAsync(string scenarioNodeId, string configurationNodeId)
    {
        return await WalkInternalAsync(scenarioNodeId, configurationNodeId);
    }

    // ── Core traversal ────────────────────────────────────────────────────────

    private async Task<WalkResult> WalkInternalAsync(string scenarioNodeId, string? configurationNodeId)
    {
        await using var session = _repo.OpenSession();

        // 1. Resolve scenario — get solve-for mode and root parameter node
        var (scenario, rootParameterId, baseValues) = await ResolveScenarioAsync(session, scenarioNodeId);

        // 2. If configuration provided, get overrides and merge onto base values
        var effectiveValues = new Dictionary<string, double>(baseValues);
        if (configurationNodeId is not null)
        {
            var overrides = await GetOverrideMapAsync(session, configurationNodeId);
            foreach (var (paramId, value) in overrides)
                effectiveValues[paramId] = value;
        }

        // 3. Load full subtree from Neo4j
        var subtree = await LoadSubtreeAsync(session, rootParameterId);

        // 4. Recursive rollup
        var rootResult = EvaluateNode(rootParameterId, subtree, effectiveValues, scenario.SolveForMode);

        return new WalkResult(
            scenarioNodeId,
            configurationNodeId,
            scenario.SolveForMode,
            rootResult,
            double.IsNaN(rootResult.EffectiveValue ?? double.NaN) ? null : rootResult.EffectiveValue,
            DateTimeOffset.UtcNow
        );
    }

    // ── Node evaluation (recursive) ───────────────────────────────────────────

    private NodeResult EvaluateNode(
        string nodeId,
        SubtreeData subtree,
        Dictionary<string, double> effectiveValues,
        SolveForMode solveForMode)
    {
        var node = subtree.Nodes[nodeId];
        var children = subtree.EdgesByParent.GetValueOrDefault(nodeId, new());

        if (children.Count == 0)
        {
            // Leaf node — value comes from effectiveValues
            var isRelevant = IsNodeRelevantToSolveFor(node.Role, solveForMode);
            var leafValue = isRelevant && effectiveValues.TryGetValue(nodeId, out var v) ? v : (double?)null;

            return new NodeResult(
                nodeId, node.Name, node.Role,
                leafValue, 1.0, "Leaf", IsLeaf: true, Children: new()
            );
        }

        // Internal node — evaluate children and rollup
        var childResults = children.Select(edge =>
            EvaluateNode(edge.ChildNodeId, subtree, effectiveValues, solveForMode)
        ).ToList();

        var rollupOperator = node.RollupOperator ?? "WeightedSum";
        var rolledUpValue = Rollup(childResults, children, rollupOperator);

        return new NodeResult(
            nodeId, node.Name, node.Role,
            rolledUpValue, 1.0, rollupOperator, IsLeaf: false, Children: childResults
        );
    }

    // ── Rollup math ───────────────────────────────────────────────────────────

    /// <summary>
    /// Applies the rollup operator to aggregate child values.
    /// The walker itself is dumb — it follows the operator on each edge.
    /// Adding new process topologies requires no changes here.
    /// Track 2 will extend this with the formal ECT math rewrite.
    /// </summary>
    private static double? Rollup(
        List<NodeResult> children,
        List<EdgeData> edges,
        string operatorName)
    {
        // Exclude N/A children from rollup
        var activeChildren = children
            .Zip(edges, (c, e) => (result: c, edge: e))
            .Where(x => x.result.EffectiveValue.HasValue)
            .ToList();

        if (activeChildren.Count == 0) return null;

        return operatorName switch
        {
            "Sum" =>
                activeChildren.Sum(x => x.result.EffectiveValue!.Value),

            "Product" =>
                activeChildren.Aggregate(1.0, (acc, x) => acc * x.result.EffectiveValue!.Value),

            "WeightedSum" =>
                activeChildren.Sum(x => x.result.EffectiveValue!.Value * x.edge.Weight),

            "Max" =>
                activeChildren.Max(x => x.result.EffectiveValue!.Value),

            "Min" =>
                activeChildren.Min(x => x.result.EffectiveValue!.Value),

            _ => throw new InvalidOperationException($"Unknown rollup operator: {operatorName}")
        };
    }

    // ── Solve-for relevance ───────────────────────────────────────────────────

    /// <summary>
    /// Determines whether a node's role contributes a value vs. returns N/A
    /// for the given solve-for mode. The compute target node type is the output,
    /// not an input — its children must be inputs of the other types.
    ///
    /// NOTE: Full solve-for semantics will be formalised in Track 2.
    /// This is a structural placeholder that correctly gates leaf node values.
    /// </summary>
    private static bool IsNodeRelevantToSolveFor(ParameterRole role, SolveForMode mode)
    {
        return mode switch
        {
            SolveForMode.C    => role is ParameterRole.E or ParameterRole.T,
            SolveForMode.T    => role is ParameterRole.E or ParameterRole.C,
            SolveForMode.E    => role is ParameterRole.C or ParameterRole.T,
            SolveForMode.T_ET => role is ParameterRole.E or ParameterRole.C or ParameterRole.k,
            SolveForMode.C_ET => role is ParameterRole.E or ParameterRole.T or ParameterRole.k,
            SolveForMode.EC   => role is ParameterRole.T or ParameterRole.k,
            _ => true
        };
    }

    // ── Neo4j data loading helpers ────────────────────────────────────────────

    private async Task<(ScenarioNode scenario, string rootParameterId, Dictionary<string, double> baseValues)>
        ResolveScenarioAsync(IAsyncSession session, string scenarioNodeId)
    {
        var cursor = await session.RunAsync(
            CypherQueries.GetUsesEdgeForScenario,
            new { scenarioNodeId });

        var record = await cursor.SingleAsync();

        var sNode = record["s"].As<INode>();
        var rEdge = record["r"].As<IRelationship>();
        var pNode = record["p"].As<INode>();

        var scenario = MapScenarioNode(sNode);
        var rootParameterId = pNode["id"].As<string>();

        // Base values are stored as a JSON string on the USES edge
        var valuesJson = rEdge.Properties.TryGetValue("baseParameterValues", out var bpv)
            ? bpv?.As<string>() : null;

        var baseValues = valuesJson is not null
            ? System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, double>>(valuesJson)
              ?? new Dictionary<string, double>()
            : new Dictionary<string, double>();

        return (scenario, rootParameterId, baseValues);
    }

    private async Task<Dictionary<string, double>> GetOverrideMapAsync(
        IAsyncSession session, string configurationNodeId)
    {
        var cursor = await session.RunAsync(
            CypherQueries.GetOverrideMapForConfiguration,
            new { configurationNodeId });

        var overrides = new Dictionary<string, double>();
        await foreach (var record in cursor)
        {
            var paramId = record["parameterId"].As<string>();
            var value = record["overrideValue"].As<double>();
            overrides[paramId] = value;
        }
        return overrides;
    }

    private async Task<SubtreeData> LoadSubtreeAsync(IAsyncSession session, string rootId)
    {
        var cursor = await session.RunAsync(
            CypherQueries.GetSubtree,
            new { rootId });

        var nodes = new Dictionary<string, NodeData>();
        // Key: Neo4j element ID → our id property UUID (for resolving edge endpoints)
        var elementIdToOurId = new Dictionary<string, string>();
        var edgesByParent = new Dictionary<string, List<EdgeData>>();

        await foreach (var record in cursor)
        {
            var pathNodes = record["pathNodes"].As<List<INode>>();
            var pathEdges = record["pathEdges"].As<List<IRelationship>>();

            // First pass — collect all nodes and build element ID → our ID map
            foreach (var n in pathNodes)
            {
                var ourId = n["id"].As<string>();
                if (!nodes.ContainsKey(ourId))
                {
                    nodes[ourId] = MapNodeData(n);
                    elementIdToOurId[n.ElementId] = ourId;
                }
            }

            // Second pass — build edge map using our IDs
            foreach (var e in pathEdges)
            {
                // CONTRIBUTES_TO goes child → parent
                // StartNode = child (from), EndNode = parent (to)
                if (!elementIdToOurId.TryGetValue(e.EndNodeElementId, out var parentOurId))
                    continue;
                if (!elementIdToOurId.TryGetValue(e.StartNodeElementId, out var childOurId))
                    continue;

                if (!edgesByParent.ContainsKey(parentOurId))
                    edgesByParent[parentOurId] = new();

                edgesByParent[parentOurId].Add(new EdgeData(
                    childOurId,
                    e.Properties.TryGetValue("rollupOperator", out var op) ? op.As<string>() : "WeightedSum",
                    e.Properties.TryGetValue("weight", out var w) ? w.As<double>() : 1.0
                ));
            }
        }

        return new SubtreeData(nodes, edgesByParent);
    }

    // ── Mapping helpers ───────────────────────────────────────────────────────

    private static NodeData MapNodeData(INode n) => new(
        n["id"].As<string>(),
        n["name"].As<string>(),
        Enum.Parse<ParameterRole>(n["role"].As<string>()),
        n.Properties.TryGetValue("rollupOperator", out var op) && op is not null
            ? op.As<string>() : null
    );

    private static ScenarioNode MapScenarioNode(INode n) => new()
    {
        Id = n["id"].As<string>(),
        Name = n["name"].As<string>(),
        SolveForMode = Enum.Parse<SolveForMode>(n["solveForMode"].As<string>()),
        Domain = n["domain"].As<string>(),
        ExternalScenarioId = n.Properties.TryGetValue("externalScenarioId", out var eid)
            ? eid?.As<string>() : null,
    };

    // ── Internal data structures ──────────────────────────────────────────────

    private record NodeData(string Id, string Name, ParameterRole Role, string? RollupOperator);
    private record EdgeData(string ChildNodeId, string RollupOperator, double Weight);
    private record SubtreeData(
        Dictionary<string, NodeData> Nodes,
        Dictionary<string, List<EdgeData>> EdgesByParent);
}
