using Temporalio.Common;
using Temporalio.Workflows;
using WorkflowEval.ApiService.Activities;
using Mutex = Temporalio.Workflows.Mutex;

namespace WorkflowEval.ApiService;

[Workflow]
public class MyWorkflow : IMyWorkflow
{
    private readonly Mutex _mutex = new();

    [WorkflowSignal("approve")]
    public async Task ApproveAsync(string text)
    {
        // by default signals processing concurrently
        // but temporal allows u to ensure that only one signal processing
        // at this moment with some of built in features, like mutex
        
        await _mutex.WaitOneAsync();

        try
        {
            await Workflow.DelayAsync(TimeSpan.FromSeconds(Random.Shared.Next(0, 5)));
            Workflow.Logger.LogInformation("Signal received: {Data}", text);
        }
        finally
        {
            _mutex.ReleaseMutex();
        }
    }
    
    [WorkflowRun]
    public async Task<string> RunAsync()
    {
        // Run an async instance method activity.
        var result1 = await Workflow.ExecuteActivityAsync(
            (SomeActivities act) => act.FirstAsync(),
            new ActivityOptions
            {
                RetryPolicy = new RetryPolicy
                {
                    MaximumAttempts = 3
                },
                StartToCloseTimeout = TimeSpan.FromMinutes(5),
            });

        Workflow.Logger.LogInformation("Activity instance method result: {Result}", result1);

        await Workflow.ExecuteActivityAsync(
            (SomeActivities act) => act.SecondAsync(result1),
            new()
            {
                StartToCloseTimeout = TimeSpan.FromMinutes(5),
            });
        
        Workflow.Logger.LogInformation("Activity instance method.");
            
        return result1;
    }
}