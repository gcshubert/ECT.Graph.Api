namespace ECT.Graph.Api.Configuration;

public class Neo4jSettings
{
    public string Uri { get; set; } = "bolt://localhost:7687";
    public string Username { get; set; } = "neo4j";
    public string Password { get; set; } = string.Empty;
    public string Database { get; set; } = "neo4j";
}
