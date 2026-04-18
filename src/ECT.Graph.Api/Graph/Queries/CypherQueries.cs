namespace ECT.Graph.Api.Graph.Queries;

/// <summary>
/// All Cypher queries used by the application.
/// Centralised here so they are easy to audit, test, and profile in Neo4j Browser.
/// Parameters are always passed as named $param values — never string-interpolated.
/// </summary>
public static class CypherQueries
{
    // ── ParameterNode ─────────────────────────────────────────────────────────

    public const string CreateParameterNode = @"
        CREATE (n:ParameterNode {
            id:             $id,
            name:           $name,
            role:           $role,
            rollupOperator: $rollupOperator,
            description:    $description,
            isActive:       $isActive,
            coefficient:    $coefficient,
            exponent:       $exponent,
            provenance:     $provenance,
            externalScenarioId: $externalScenarioId,
            eCoefficient:   $eCoefficient,
            eExponent:      $eExponent,
            cCoefficient:   $cCoefficient,
            cExponent:      $cExponent,
            kCoefficient:   $kCoefficient,
            kExponent:      $kExponent,
            tCoefficient:   $tCoefficient,
            tExponent:      $tExponent,
            eProvenance:    $eProvenance,
            cProvenance:    $cProvenance,
            kProvenance:    $kProvenance,
            tProvenance:    $tProvenance
        })
        RETURN n";

    public const string GetParameterNodeById = @"
        MATCH (n:ParameterNode {id: $id})
        RETURN n";

    public const string GetParameterNodesByScenarioId = @"
        MATCH (n:ParameterNode {externalScenarioId: $scenarioId})
        RETURN n AS node
        ORDER BY n.name";

    public const string GetParameterNodesByScenarioRoot = @"
        MATCH (root:ParameterNode {id: $rootId})
        MATCH (root)<-[:CONTRIBUTES_TO*0..]-(n:ParameterNode)
        RETURN DISTINCT n AS node
        ORDER BY n.name";

    public const string GetAllParameterNodes = @"
        MATCH (n:ParameterNode)
        RETURN n
        ORDER BY n.name";

    public const string UpdateParameterNode = @"
        MATCH (n:ParameterNode {id: $id})
        SET n.name           = $name,
            n.role           = $role,
            n.rollupOperator = $rollupOperator,
            n.description    = $description,
            n.isActive       = $isActive,
            n.coefficient    = $coefficient,
            n.exponent       = $exponent,
            n.provenance     = $provenance,
            n.externalScenarioId = $externalScenarioId,
            n.eCoefficient   = $eCoefficient,
            n.eExponent      = $eExponent,
            n.cCoefficient   = $cCoefficient,
            n.cExponent      = $cExponent,
            n.kCoefficient   = $kCoefficient,
            n.kExponent      = $kExponent,
            n.tCoefficient   = $tCoefficient,
            n.tExponent      = $tExponent,
            n.eProvenance    = $eProvenance,
            n.cProvenance    = $cProvenance,
            n.kProvenance    = $kProvenance,
            n.tProvenance    = $tProvenance
        RETURN n";

    public const string DeleteParameterNode = @"
        MATCH (n:ParameterNode {id: $id})
        DETACH DELETE n";

    public const string DeleteStepWithLeaves = @"
        MATCH (anchor:ParameterNode {id: $anchorId})
        OPTIONAL MATCH (leaf:ParameterNode)-[:CONTRIBUTES_TO]->(anchor)
        DETACH DELETE anchor, leaf";

    public const string DeleteAllParameterNodesForScenario = @"
        MATCH (n:ParameterNode {externalScenarioId: $scenarioId})
        DETACH DELETE n";
    // ── ScenarioNode ──────────────────────────────────────────────────────────

    public const string CreateScenarioNode = @"
        CREATE (n:ScenarioNode {
            id:                 $id,
            name:               $name,
            solveForMode:       $solveForMode,
            domain:             $domain,
            externalScenarioId: $externalScenarioId,
            description:        $description
        })
        RETURN n";

    public const string GetScenarioNodeById = @"
        MATCH (n:ScenarioNode {id: $id})
        RETURN n";

    public const string GetScenarioNodeByExternalId = @"
        MATCH (n:ScenarioNode {externalScenarioId: $externalScenarioId})
        RETURN n";

    public const string GetAllScenarioNodes = @"
        MATCH (n:ScenarioNode)
        RETURN n
        ORDER BY n.name";

    public const string UpdateScenarioNode = @"
        MATCH (n:ScenarioNode {id: $id})
        SET n.name               = $name,
            n.solveForMode       = $solveForMode,
            n.domain             = $domain,
            n.externalScenarioId = $externalScenarioId,
            n.description        = $description
        RETURN n";

    public const string DeleteScenarioNode = @"
        MATCH (n:ScenarioNode {id: $id})
        DETACH DELETE n";

    // ── ConfigurationNode ─────────────────────────────────────────────────────

    public const string CreateConfigurationNode = @"
        CREATE (n:ConfigurationNode {
            id:                       $id,
            name:                     $name,
            scenarioNodeId:           $scenarioNodeId,
            externalConfigurationId:  $externalConfigurationId,
            description:              $description
        })
        RETURN n";

    public const string GetConfigurationNodeById = @"
        MATCH (n:ConfigurationNode {id: $id})
        RETURN n";

    public const string GetConfigurationsByScenarioId = @"
        MATCH (n:ConfigurationNode)-[:BELONGS_TO]->(s:ScenarioNode {id: $scenarioNodeId})
        RETURN n
        ORDER BY n.name";

    public const string DeleteConfigurationNode = @"
        MATCH (n:ConfigurationNode {id: $id})
        DETACH DELETE n";

    // ── CONTRIBUTES_TO edges ──────────────────────────────────────────────────

    public const string CreateContributesToEdge = @"
        MATCH (from:ParameterNode {id: $fromId})
        MATCH (to:ParameterNode   {id: $toId})
        CREATE (from)-[r:CONTRIBUTES_TO {
            id:             $id,
            rollupOperator: $rollupOperator,
            weight:         $weight,
            `sortOrder`:    $sortOrder
        }]->(to)
        RETURN r";

    // ── Hierarchy parent lookup ───────────────────────────────────────────────

    /// <summary>
    /// Returns all CONTRIBUTES_TO edges for a given scenario's parameter topology.
    /// Used to reconstruct parent/child relationships in the hierarchy tree.
    /// </summary>
    public const string GetAllContributesToEdges = @"
    MATCH (child:ParameterNode)-[r:CONTRIBUTES_TO]->(parent:ParameterNode)
    RETURN child.id AS childId, parent.id AS parentId, r.weight AS weight, r.rollupOperator AS rollupOperator, r.id AS id, r.sortOrder AS sortOrder
    ORDER BY r.sortOrder ASC";

    /// <summary>
    /// Returns all CONTRIBUTES_TO edges reachable from a scenario's root ParameterNode.
    /// Scoped to a single scenario via the root node id.
    /// </summary>
    public const string GetContributesToEdgesByScenarioRoot = @"
    MATCH (root:ParameterNode {id: $rootId})<-[:CONTRIBUTES_TO*1..]-(child:ParameterNode)
    MATCH (child)-[r:CONTRIBUTES_TO]->(parent:ParameterNode)
    RETURN child.id AS childId, parent.id AS parentId, r.weight AS weight,
           r.rollupOperator AS rollupOperator, r.id AS id, r.sortOrder AS sortOrder
    ORDER BY r.sortOrder ASC";

    public const string GetContributesToEdgesByScenarioId = @"
    MATCH (child:ParameterNode {externalScenarioId: $scenarioId})-[r:CONTRIBUTES_TO]->(parent:ParameterNode)
    RETURN child.id AS childId, parent.id AS parentId, r.weight AS weight,
           r.rollupOperator AS rollupOperator, r.id AS id, r.sortOrder AS sortOrder
    ORDER BY r.sortOrder ASC";

    public const string GetMaxSortOrderForParent = @"
    MATCH (child:ParameterNode)-[r:CONTRIBUTES_TO]->(parent:ParameterNode {id: $parentId})
    RETURN coalesce(max(r.sortOrder), 0) AS maxSortOrder";

    public const string GetContributorsOf = @"
        MATCH (child:ParameterNode)-[r:CONTRIBUTES_TO]->(parent:ParameterNode {id: $parentId})
        RETURN child, r";

    public const string DeleteContributesToEdge = @"
        MATCH ()-[r:CONTRIBUTES_TO {id: $id}]-()
        DELETE r";

    // ── DEPENDS_ON edges ─────────────────────────────────────────────────────

    public const string CreateDependsOnEdge = @"
        MATCH (from:ParameterNode {id: $fromId})
        MATCH (to:ParameterNode   {id: $toId})
        CREATE (from)-[r:DEPENDS_ON {
            id:             $id,
            description:    $description,
            strength:       $strength,
            `sortOrder`:    $sortOrder
        }]->(to)
        RETURN r";

    public const string GetDependsOnEdgeById = @"
        MATCH ()-[r:DEPENDS_ON {id: $id}]-()
        RETURN r";

    public const string GetAllDependsOnEdges = @"
        MATCH (from:ParameterNode)-[r:DEPENDS_ON]->(to:ParameterNode)
        RETURN from.id AS fromId, to.id AS toId, r.description AS description, r.strength AS strength, r.id AS id, r.sortOrder AS sortOrder
        ORDER BY r.sortOrder ASC";

    public const string DeleteDependsOnEdge = @"
        MATCH ()-[r:DEPENDS_ON {id: $id}]-()
        DELETE r";

    // ── USES edges ────────────────────────────────────────────────────────────

    public const string CreateUsesEdge = @"
        MATCH (s:ScenarioNode    {id: $scenarioNodeId})
        MATCH (p:ParameterNode   {id: $rootParameterNodeId})
        CREATE (s)-[r:USES {
            id:                   $id,
            baseParameterValues:  $baseParameterValues
        }]->(p)
        RETURN r";

    public const string GetUsesEdgeForScenario = @"
        MATCH (s:ScenarioNode {id: $scenarioNodeId})-[r:USES]->(p:ParameterNode)
        RETURN s, r, p";

    public const string DeleteUsesEdgesForScenario = @"
        MATCH (s:ScenarioNode {id: $scenarioNodeId})-[r:USES]->()
        DELETE r";

    // ── BELONGS_TO edges ──────────────────────────────────────────────────────

    public const string CreateBelongsToEdge = @"
        MATCH (c:ConfigurationNode {id: $configurationNodeId})
        MATCH (s:ScenarioNode      {id: $scenarioNodeId})
        CREATE (c)-[r:BELONGS_TO {id: $id}]->(s)
        RETURN r";

    // ── OVERRIDES edges ───────────────────────────────────────────────────────

    public const string CreateOverridesEdge = @"
        MATCH (c:ConfigurationNode {id: $configurationNodeId})
        MATCH (p:ParameterNode     {id: $parameterNodeId})
        CREATE (c)-[r:OVERRIDES {
            id:            $id,
            overrideValue: $overrideValue
        }]->(p)
        RETURN r";

    public const string GetOverridesForConfiguration = @"
        MATCH (c:ConfigurationNode {id: $configurationNodeId})-[r:OVERRIDES]->(p:ParameterNode)
        RETURN p, r";

    public const string DeleteOverridesEdge = @"
        MATCH ()-[r:OVERRIDES {id: $id}]-()
        DELETE r";

    // ── Walker queries ────────────────────────────────────────────────────────

    /// <summary>
    /// Full subtree walk from a root ParameterNode.
    /// Returns all nodes and CONTRIBUTES_TO edges reachable from the given root.
    /// apoc.path.subgraphAll is the cleanest approach but requires APOC plugin.
    /// This native alternative uses variable-length path matching.
    /// Edge type filter applied: only CONTRIBUTES_TO edges are traversed.
    /// </summary>
    public const string GetSubtree = @"
        MATCH path = (root:ParameterNode {id: $rootId})<-[:CONTRIBUTES_TO*0..]-(leaf:ParameterNode)
        RETURN nodes(path) AS pathNodes, relationships(path) AS pathEdges";

    /// <summary>
    /// Retrieve all OVERRIDES for a configuration as a map of parameterNodeId → overrideValue.
    /// Used by walker to build the effective value set before traversal.
    /// </summary>
    public const string GetOverrideMapForConfiguration = @"
        MATCH (c:ConfigurationNode {id: $configurationNodeId})-[r:OVERRIDES]->(p:ParameterNode)
        RETURN p.id AS parameterId, r.overrideValue AS overrideValue";
}
