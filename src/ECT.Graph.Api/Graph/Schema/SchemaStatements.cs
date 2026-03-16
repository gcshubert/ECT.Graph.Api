namespace ECT.Graph.Api.Graph.Schema;

/// <summary>
/// Cypher statements that define the Neo4j schema for the ECT graph.
/// Applied idempotently on service startup via GraphRepository.ApplySchemaConstraintsAsync().
///
/// Design notes:
///   - Node uniqueness is enforced on the Id property for all node types.
///   - Indexes are added on frequently-queried properties (Name, Role, SolveForMode).
///   - Edge properties (weight, operator, values) are not separately indexed —
///     they are accessed via traversal, not standalone lookup.
///   - All constraints use IF NOT EXISTS so repeated startup calls are safe.
/// </summary>
public static class SchemaStatements
{
    public static IEnumerable<string> All => new[]
    {
        // ── Uniqueness constraints ────────────────────────────────────────────

        // ParameterNode
        "CREATE CONSTRAINT parameter_node_id IF NOT EXISTS " +
        "FOR (n:ParameterNode) REQUIRE n.id IS UNIQUE",

        // ScenarioNode
        "CREATE CONSTRAINT scenario_node_id IF NOT EXISTS " +
        "FOR (n:ScenarioNode) REQUIRE n.id IS UNIQUE",

        // ConfigurationNode
        "CREATE CONSTRAINT configuration_node_id IF NOT EXISTS " +
        "FOR (n:ConfigurationNode) REQUIRE n.id IS UNIQUE",

        // ── Lookup indexes ────────────────────────────────────────────────────

        // ParameterNode.name — queried when resolving by well-known parameter names
        "CREATE INDEX parameter_node_name IF NOT EXISTS " +
        "FOR (n:ParameterNode) ON (n.name)",

        // ParameterNode.role — queried during traversal to filter by E/T/C/k
        "CREATE INDEX parameter_node_role IF NOT EXISTS " +
        "FOR (n:ParameterNode) ON (n.role)",

        // ScenarioNode.externalScenarioId — queried by ECT.ACC.Api to resolve by its own IDs
        "CREATE INDEX scenario_external_id IF NOT EXISTS " +
        "FOR (n:ScenarioNode) ON (n.externalScenarioId)",

        // ScenarioNode.solveForMode — queried when filtering scenarios by mode
        "CREATE INDEX scenario_solve_for_mode IF NOT EXISTS " +
        "FOR (n:ScenarioNode) ON (n.solveForMode)",

        // ScenarioNode.domain — queried by ACC-H classifier
        "CREATE INDEX scenario_domain IF NOT EXISTS " +
        "FOR (n:ScenarioNode) ON (n.domain)",

        // ConfigurationNode.externalConfigurationId — resolved by ECT.ACC.Api
        "CREATE INDEX configuration_external_id IF NOT EXISTS " +
        "FOR (n:ConfigurationNode) ON (n.externalConfigurationId)",
    };
}
