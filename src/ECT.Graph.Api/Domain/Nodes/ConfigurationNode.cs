namespace ECT.Graph.Api.Domain.Nodes;

/// <summary>
/// Represents a configuration run within a scenario.
/// Connected to its ScenarioNode via BELONGS_TO.
/// Connected to specific ParameterNodes it overrides via OVERRIDES edges (which carry the override values).
/// </summary>
public class ConfigurationNode
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// ID of the ScenarioNode this configuration belongs to.
    /// Stored here for convenience; the authoritative link is the BELONGS_TO edge in Neo4j.
    /// </summary>
    public string ScenarioNodeId { get; set; } = string.Empty;

    /// <summary>
    /// Optional reference back to the configuration record in ECT.ACC.Api (SQL Server).
    /// </summary>
    public string? ExternalConfigurationId { get; set; }

    public string? Description { get; set; }
}
