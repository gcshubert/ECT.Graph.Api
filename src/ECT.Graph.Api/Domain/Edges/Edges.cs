namespace ECT.Graph.Api.Domain.Edges;

/// <summary>
/// CONTRIBUTES_TO — between ParameterNodes.
/// Carries rollup operator and weight. Defines the process topology tree.
/// Multiple outbound CONTRIBUTES_TO edges per node are permitted (DAG extension point —
/// start with strict tree; flag multi-parent as future extension).
/// </summary>
public class ContributesToEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Source ParameterNode Id (child / contributor).</summary>
    public string FromParameterNodeId { get; set; } = string.Empty;

    /// <summary>Target ParameterNode Id (parent / aggregate).</summary>
    public string ToParameterNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Rollup operator applied to this contribution.
    /// Overrides the parent node's default operator for this specific edge if set.
    /// </summary>
    public string RollupOperator { get; set; } = "WeightedSum";

    /// <summary>
    /// Fractional weight of this contribution in the parent rollup (0.0–1.0).
    /// Weights across all CONTRIBUTES_TO edges into a parent should sum to 1.0
    /// for WeightedSum operators.
    /// </summary>
    public double Weight { get; set; } = 1.0;
}

/// <summary>
/// USES — ScenarioNode to root ParameterNode.
/// Establishes which topology a scenario operates over.
/// Carries base parameter values for the scenario — keeping values off ParameterNode
/// so the same topology can be shared across scenarios with different values.
/// </summary>
public class UsesEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string ScenarioNodeId { get; set; } = string.Empty;

    /// <summary>Root ParameterNode Id — entry point for tree traversal.</summary>
    public string RootParameterNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Base parameter values for this scenario, keyed by ParameterNode Id.
    /// These are the unoverridden values used when no ConfigurationNode override applies.
    /// </summary>
    public Dictionary<string, double> BaseParameterValues { get; set; } = new();
}

/// <summary>
/// BELONGS_TO — ConfigurationNode to ScenarioNode.
/// </summary>
public class BelongsToEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string ConfigurationNodeId { get; set; } = string.Empty;
    public string ScenarioNodeId { get; set; } = string.Empty;
}

/// <summary>
/// OVERRIDES — ConfigurationNode to ParameterNode.
/// Carries the override value for a specific parameter in a specific configuration run.
/// </summary>
public class OverridesEdge
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string ConfigurationNodeId { get; set; } = string.Empty;
    public string ParameterNodeId { get; set; } = string.Empty;

    /// <summary>
    /// The override value that replaces the scenario base value for this parameter
    /// during a configuration run.
    /// </summary>
    public double OverrideValue { get; set; }
}
