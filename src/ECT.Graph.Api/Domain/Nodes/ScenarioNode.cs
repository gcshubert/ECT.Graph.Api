namespace ECT.Graph.Api.Domain.Nodes;

/// <summary>
/// Represents an ECT scenario operating over a parameter topology.
/// Base parameter values are carried on USES edges into the parameter graph — NOT here.
/// This keeps the topology reusable across scenarios with different base values.
/// </summary>
public class ScenarioNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Display name for the scenario.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Which parameter type is the compute target for this scenario.
    /// All other types are consumed as inputs during traversal.
    /// </summary>
    public SolveForMode SolveForMode { get; set; }

    /// <summary>
    /// Application domain (e.g., "Biological_ACC", "Thermal", "LaserCut").
    /// Drives domain-specific threshold selection in the ACC-H classifier.
    /// </summary>
    public string Domain { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference back to the scenario record in ECT.ACC.Api (SQL Server).
    /// Allows ECT.ACC.Api to resolve graph results by its own scenario IDs.
    /// </summary>
    public string? ExternalScenarioId { get; set; }

    public string? Description { get; set; }
}

public enum SolveForMode
{
    C,      // Classic ACC — control deficit analysis (known: E, T)
    T,      // Time window optimization (known: E, C)
    E,      // Energy requirement derivation (known: C, T)
    T_ET,   // Throughput optimization (known: E×C, k)
    C_ET,   // Precision optimization (known: E×T, k)
    EC      // Combined energy-control budget (known: T, k)
}
