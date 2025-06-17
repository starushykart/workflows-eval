using Temporalio.Workflows;

namespace WorkflowEval.ApiService.Temporal;

[Workflow]
public interface IMyWorkflow
{
    [WorkflowRun]
    Task<string> RunAsync();
}