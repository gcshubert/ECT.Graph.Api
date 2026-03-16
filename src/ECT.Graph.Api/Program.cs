using ECT.Graph.Api.Configuration;
using ECT.Graph.Api.Graph.Walker;
using ECT.Graph.Api.Infrastructure;
using ECT.Graph.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Neo4j
builder.Services.Configure<Neo4jSettings>(builder.Configuration.GetSection("Neo4j"));
builder.Services.AddSingleton<INeo4jDriverFactory, Neo4jDriverFactory>();
builder.Services.AddSingleton<IGraphRepository, GraphRepository>();

// Domain services
builder.Services.AddScoped<IParameterNodeService, ParameterNodeService>();
builder.Services.AddScoped<IScenarioNodeService, ScenarioNodeService>();
builder.Services.AddScoped<IConfigurationNodeService, ConfigurationNodeService>();
builder.Services.AddScoped<IEdgeService, EdgeService>();
builder.Services.AddScoped<IGraphWalker, GraphWalker>();

// API
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter());
        opts.JsonSerializerOptions.NumberHandling =
            System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "ECT.Graph.Api",
        Version = "v1",
        Description = "Graph repository service for ECT parameter topology, rollup traversal, and solve-for analysis."
    });
    c.UseInlineDefinitionsForEnums();
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) c.IncludeXmlComments(xmlPath);
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "ECT.Graph.Api v1"));

app.UseHttpsRedirection();
app.MapControllers();

// On startup, apply Neo4j schema constraints
using (var scope = app.Services.CreateScope())
{
    var repo = scope.ServiceProvider.GetRequiredService<IGraphRepository>();
    await repo.ApplySchemaConstraintsAsync();
}

app.Run();
