using WorkflowEval.AppHost.Extensions;

var builder = DistributedApplication.CreateBuilder(args);

var cache = builder.AddRedis("cache");

var localstack = builder.AddLocalStack();

var postgres = builder.AddPostgres("postgres").WithPgWeb();
var postgresDb = postgres.AddDatabase("postgresdb");

var apiService = builder
    .AddProject<Projects.WorkflowEval_ApiService>("apiservice")
    .WithReference(postgresDb)
    .WaitFor(postgresDb)
    .WaitFor(localstack)
    .WithHttpHealthCheck("/health");

apiService.AddSwaggerUiCommand();

builder.Build().Run();
