using Amazon.SimpleNotificationService;
using Amazon.SQS;
using MassTransit;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;
using WorkflowEval.ApiService;
using WorkflowEval.ApiService.Persistence;
using WorkflowEval.ApiService.Saga;
using WorkflowEval.ApiService.Saga.Events;
using WorkflowEval.ApiService.Temporal;
using WorkflowEval.ApiService.Temporal.Activities;
using WorkflowEval.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHostedService<MigrationHostedService>();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors();

builder.Services.AddDbContext<AppDbContext>(x => 
    x.UseNpgsql(builder.Configuration.GetConnectionString("postgresdb")));

builder.Services.AddMassTransit(x =>
{
    x.AddEntityFrameworkOutbox<AppDbContext>(o =>
    {
        o.UsePostgres();
        o.UseBusOutbox();
    });

    x.AddSagaStateMachine<OrderStateMachine, OrderState>()
        .EntityFrameworkRepository(r =>
        {
            r.ConcurrencyMode = ConcurrencyMode.Optimistic;
            r.ExistingDbContext<AppDbContext>();
        });
    
    x.UsingAmazonSqs((context, cfg) =>
    {
        cfg.Host(new Uri("amazonsqs://localhost:4566"), h =>
        {
            h.AccessKey("admin");
            h.SecretKey("admin");

            h.Config(new AmazonSQSConfig { ServiceURL = "http://localhost:4566" });
            h.Config(new AmazonSimpleNotificationServiceConfig { ServiceURL = "http://localhost:4566" });
        });
        cfg.ConfigureEndpoints(context);
    });
});

builder.Services.AddTemporalClient(opt =>
    {
        opt.TargetHost = "127.0.0.1:7233";
        opt.Namespace = "default";
        opt.Interceptors = [new TracingInterceptor()];
    })
    .AddHostedTemporalWorker("1")
    .AddWorkflow<MyWorkflow>()
    .AddScopedActivities<SomeActivities>();

var app = builder.Build();

app.UseExceptionHandler();

app.UseCors(x=>x.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin());
app.MapOpenApi();
app.UseSwaggerUI(x => x.SwaggerEndpoint("/openapi/v1.json", "OpenApi v1.1"));

app.MapGet("temporal/workflow", async ([FromServices]ITemporalClient client) =>
    {
        var workflow = await client.StartWorkflowAsync((IMyWorkflow w) => w.RunAsync(),
            new WorkflowOptions { Id = Guid.NewGuid().ToString(), TaskQueue = "1" });

        return workflow.Id;
    })
.WithName("Temporal Submit");

app.MapGet("temporal/workflow-signal", async ([FromServices]ITemporalClient client, string workflowId) =>
    {
        // send some signals to workflow
        // like events, progress updates from external workers, etc
        var handle = client.GetWorkflowHandle(workflowId);

        for (var i = 0; i < 5; i++)
            await handle.SignalAsync("approve", [$"signal {i} - {DateTime.UtcNow}"]);
    })
    .WithName("Temporal Signal");

app.MapGet("masstransit/submit", async ([FromServices]IPublishEndpoint publisher, [FromServices] AppDbContext context) =>
    {
        var orderId = Guid.NewGuid();
        await publisher.Publish(new OrderSubmitted(orderId));
        context.Orders.Add(new Order { Id = orderId, Progress = 0 });
        await context.SaveChangesAsync();
        return orderId;
    })
    .WithName("MT Saga Submit");

app.MapGet("masstransit/progress", async ([FromServices]IPublishEndpoint publisher, [FromServices] AppDbContext context, Guid orderId, int progress) =>
    {
        await publisher.Publish(new OrderProgress(orderId, progress));
        await context.SaveChangesAsync();
    })
    .WithName("MT Saga Progress");

app.MapDefaultEndpoints();

app.Run();
