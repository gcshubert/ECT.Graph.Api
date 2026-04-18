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

    // ─────────────────────────────────────────────────────────────
    // Decision 1 — Scientific values stored directly on leaf nodes
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Coefficient of the scientific-notation parameter value.
    /// Null for step-anchor nodes.
    /// </summary>
    public double? Coefficient { get; set; }

    /// <summary>
    /// Exponent of the scientific-notation parameter value.
    /// Null for step-anchor nodes.
    /// </summary>
    public double? Exponent { get; set; }

    /// <summary>
    /// Provenance of this parameter value:
    /// specified | spec-derived | estimated | outcome-derived | computed.
    /// Null for step-anchor nodes.
    /// </summary>
    public string? Provenance { get; set; }

    /// <summary>
    /// External scenario ID for scoping nodes to specific scenarios.
    /// Used by GetStepsAsync to filter nodes by scenario.
    /// </summary>
    public string? ExternalScenarioId { get; set; }

    // ─────────────────────────────────────────────────────────────
    // Parameters stored directly on step anchor nodes (Apr 2026)
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Energy parameter coefficient for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? ECoefficient { get; set; }

    /// <summary>
    /// Energy parameter exponent for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? EExponent { get; set; }

    /// <summary>
    /// Control parameter coefficient for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? CCoefficient { get; set; }

    /// <summary>
    /// Control parameter exponent for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? CExponent { get; set; }

    /// <summary>
    /// Complexity parameter coefficient for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? KCoefficient { get; set; }

    /// <summary>
    /// Complexity parameter exponent for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? KExponent { get; set; }

    /// <summary>
    /// Time parameter coefficient for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? TCoefficient { get; set; }

    /// <summary>
    /// Time parameter exponent for step anchor nodes.
    /// Null for leaf nodes (they use Coefficient/Exponent properties).
    /// </summary>
    public double? TExponent { get; set; }

    /// <summary>
    /// Provenance of the Energy parameter value.
    /// Null for leaf nodes.
    /// </summary>
    public string? EProvenance { get; set; }

    /// <summary>
    /// Provenance of the Control parameter value.
    /// Null for leaf nodes.
    /// </summary>
    public string? CProvenance { get; set; }

    /// <summary>
    /// Provenance of the Complexity parameter value.
    /// Null for leaf nodes.
    /// </summary>
    public string? KProvenance { get; set; }

    /// <summary>
    /// Provenance of the Time parameter value.
    /// Null for leaf nodes.
    /// </summary>
    public string? TProvenance { get; set; }

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
