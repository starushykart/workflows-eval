using System.Reflection;
using MassTransit;
using MassTransit.EntityFrameworkCoreIntegration;
using Microsoft.EntityFrameworkCore;
using WorkflowEval.ApiService.Persistence.ConfigMaps;

namespace WorkflowEval.ApiService.Persistence;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : SagaDbContext(options)
{
    public DbSet<Order> Orders => Set<Order>();

    protected override IEnumerable<ISagaClassMap> Configurations
    {
        get { yield return new OrderStateMap(); }
    }
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}