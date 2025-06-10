using Microsoft.AspNetCore.Mvc;
using Temporalio.Client;
using Temporalio.Extensions.Hosting;
using Temporalio.Extensions.OpenTelemetry;
using WorkflowEval.ApiService;
using WorkflowEval.ApiService.Activities;
using WorkflowEval.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors();

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

app.MapGet("/workflow", async ([FromServices]ITemporalClient client) =>
    {
        var workflow = await client.StartWorkflowAsync((IMyWorkflow w) => w.RunAsync(),
            new WorkflowOptions { Id = Guid.NewGuid().ToString(), TaskQueue = "1" });

        return workflow.Id;
    })
.WithName("Workflow");

app.MapGet("/workflow-signal", async ([FromServices]ITemporalClient client, string workflowId) =>
    {
        // send some signals to workflow
        // like events, progress updates from external workers, etc
        var handle = client.GetWorkflowHandle(workflowId);

        for (var i = 0; i < 5; i++)
            await handle.SignalAsync("approve", [$"signal {i} - {DateTime.UtcNow}"]);
    })
    .WithName("Workflow Signal");

app.MapDefaultEndpoints();

app.Run();
