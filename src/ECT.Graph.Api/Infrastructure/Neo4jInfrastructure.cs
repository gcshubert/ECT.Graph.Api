using ECT.Graph.Api.Configuration;
using ECT.Graph.Api.Graph.Schema;
using Microsoft.Extensions.Options;
using Neo4j.Driver;

namespace ECT.Graph.Api.Infrastructure;

// ── Driver factory ────────────────────────────────────────────────────────────

public interface INeo4jDriverFactory : IDisposable
{
    IDriver Driver { get; }
}

public class Neo4jDriverFactory : INeo4jDriverFactory
{
    private readonly IDriver _driver;

    public Neo4jDriverFactory(IOptions<Neo4jSettings> settings)
    {
        var s = settings.Value;
        _driver = GraphDatabase.Driver(
            s.Uri,
            AuthTokens.Basic(s.Username, s.Password)
        );
    }

    public IDriver Driver => _driver;

    public void Dispose() => _driver.Dispose();
}

// ── Graph repository ──────────────────────────────────────────────────────────

public interface IGraphRepository
{
    Task ApplySchemaConstraintsAsync();
    IAsyncSession OpenSession();
}

public class GraphRepository : IGraphRepository
{
    private readonly INeo4jDriverFactory _factory;
    private readonly IOptions<Neo4jSettings> _settings;
    private readonly ILogger<GraphRepository> _logger;

    public GraphRepository(
        INeo4jDriverFactory factory,
        IOptions<Neo4jSettings> settings,
        ILogger<GraphRepository> logger)
    {
        _factory = factory;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>
    /// Applies all schema constraints and indexes idempotently.
    /// Called once on startup — safe to re-run on subsequent restarts.
    /// </summary>
    public async Task ApplySchemaConstraintsAsync()
    {
        await using var session = OpenSession();
        foreach (var statement in SchemaStatements.All)
        {
            try
            {
                // 1. Run the statement
                var result = await session.RunAsync(statement);

                // 2. IMPORTANT: Explicitly consume the result metadata/summary 
                // to clear the result stream for the next iteration.
                await result.ConsumeAsync();

                _logger.LogDebug("Schema statement applied: {Statement}",
                    statement[..Math.Min(80, statement.Length)]);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to apply schema statement: {Statement}", statement);
                throw;
            }
        }
        _logger.LogInformation("Neo4j schema constraints applied successfully.");
    }
    public IAsyncSession OpenSession()
    {
        return _factory.Driver.AsyncSession(c =>
            c.WithDatabase(_settings.Value.Database));
    }
}
