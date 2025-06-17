using WorkflowEval.ApiService.Persistence;

namespace WorkflowEval.ApiService;

public class MigrationHostedService(IServiceScopeFactory factory) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = factory.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken)
    => Task.CompletedTask;
}