using System.Diagnostics;

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
    
    public static IResourceBuilder<T> AddSwaggerUiCommand<T>(this IResourceBuilder<T> resource) 
        where T: IResourceWithEndpoints
    {
        return resource.WithCommand(
            "swagger-ui-docs",
            "Swagger UI",
            async _ =>
            {
                try
                {
                    var endpoint = resource.GetEndpoint("http");
                    var url = $"{endpoint.Url}/swagger";

                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                    return new ExecuteCommandResult { Success = true };
                }
                catch (Exception ex)
                {
                    return new ExecuteCommandResult { Success = false, ErrorMessage = ex.ToString() };
                }
            },
            new CommandOptions
            {
                IconName = "Document",
                IconVariant = IconVariant.Filled
            });
    }
}