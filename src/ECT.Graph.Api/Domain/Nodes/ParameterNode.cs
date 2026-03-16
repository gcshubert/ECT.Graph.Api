namespace ECT.Graph.Api.Domain.Nodes;

/// <summary>
/// Represents a parameter in the ECT process topology.
/// Does NOT carry base values — values are scenario-specific and stored on USES edges.
/// The same ParameterNode topology can be shared across multiple scenarios.
/// </summary>
public class ParameterNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Human-readable name, e.g. "k_proc", "T_cyc", "C_ctrl".
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Typed role within ECT: E, T, C, or k.
    /// Determines which solve-for modes treat this node as a compute target vs. input.
    /// </summary>
    public ParameterRole Role { get; set; }

    /// <summary>
    /// Rollup operator applied when this node aggregates child contributions.
    /// Null for leaf nodes (no children).
    /// </summary>
    public RollupOperator? RollupOperator { get; set; }

    /// <summary>
    /// Description / annotation for documentation and agentic output.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this node is currently active in the topology.
    /// Inactive nodes return N/A during traversal without distorting rollup.
    /// </summary>
    public bool IsActive { get; set; } = true;
}

public enum ParameterRole
{
    E,  // Energy / effort input
    T,  // Time / throughput parameter
    C,  // Control capacity
    k   // Complexity ceiling
}

public enum RollupOperator
{
    Sum,
    Product,
    WeightedSum,
    Max,
    Min
}
