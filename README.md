# ECT.Graph.Api

## Neo4j password configuration

The API reads the Neo4j password from ASP.NET Core configuration. Set either:

- The `Neo4j__Password` environment variable.
- .NET user-secrets for local Development runs:

  ```sh
  dotnet user-secrets set "Neo4j:Password" "<value>" --project src/ECT.Graph.Api
  ```

The API fails during startup if `Neo4j:Password` is empty.
