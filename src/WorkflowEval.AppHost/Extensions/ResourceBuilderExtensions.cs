using System.Diagnostics;
using System.Net.Sockets;

namespace WorkflowEval.AppHost.Extensions;

public static class ResourceBuilderExtensions
{
    public static IResourceBuilder<ContainerResource> AddLocalStack(this IDistributedApplicationBuilder builder)
    {
        return builder
            .AddContainer("localstack", "localstack/localstack")
            .WithEndpoint(4566, 4566, "http", "default")
            .WithEnvironment("DEBUG", "1")
            .WithEnvironment("SERVICES", "sqs,sns");
    }
    
    public static IResourceBuilder<ContainerResource> AddTemporalCluster(this IDistributedApplicationBuilder builder,
        IResourceBuilder<PostgresDatabaseResource> postgresDb)
    {
        return builder
            .AddContainer("temporal", "temporalio/admin-tools:latest")
            .WithEndpoint(7233, 7233, name: "server")
            .AsHttp2Service()
            .WithEntrypoint("temporal")
            .WithArgs("server", "start-dev", "--namespace", "1")
            .WithEnvironment("DB", "postgres12")
            .WithEnvironment("DBNAME", postgresDb.Resource.DatabaseName)
            .WithEnvironment("DB_PORT", postgresDb.Resource.Parent.PrimaryEndpoint.TargetPort?.ToString() ?? "5432")
            .WithEnvironment("POSTGRES_USER", postgresDb.Resource.Parent.UserNameParameter?.Value ?? "postgres")
            .WithEnvironment("POSTGRES_PWD", postgresDb.Resource.Parent.PasswordParameter.Value)
            .WithEnvironment("POSTGRES_SEEDS", "postgres")
            .WithEnvironment("TEMPORAL_ADDRESS", "temporal:7233")
            .WithEnvironment("TEMPORAL_CLI_ADDRESS", "temporal:7233")
            .WaitFor(postgresDb);
    }
    
    public static IResourceBuilder<T> AddSwaggerUiCommand<T>(this IResourceBuilder<T> resource) 
        where T: IResourceWithEndpoints
    {
        return resource.WithCommand(
            "swagger-ui-docs",
            "Swagger UI", _ =>
            {
                try
                {
                    var endpoint = resource.Resource.GetEndpoint("http");
                    var url = $"{endpoint.Url}/swagger";

                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return Task.FromResult(new ExecuteCommandResult { Success = true });
                }
                catch (Exception ex)
                {
                    return Task.FromResult(new ExecuteCommandResult { Success = false, ErrorMessage = ex.ToString() });
                }
            },
            new CommandOptions
            {
                IconName = "Document",
                IconVariant = IconVariant.Filled
            });
    }
}