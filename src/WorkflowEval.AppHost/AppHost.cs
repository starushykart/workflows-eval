using WorkflowEval.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var localstack = builder.AddLocalStack();

var postgres = builder
    .AddPostgres("postgres")
    .WithPgWeb()
    .AddDatabase("postgresdb");

var temporal = await builder.AddTemporalServerContainer("temporal");

var apiService = builder
    .AddProject<Projects.WorkflowEval_ApiService>("apiservice")
    .WithReference(postgres)
    .WaitFor(postgres)
    .WaitFor(localstack)
    .WaitFor(temporal)
    .WithHttpHealthCheck("/health");

apiService.AddSwaggerUiCommand();

builder.Build().Run();
