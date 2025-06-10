using Temporalio.Workflows;

namespace WorkflowEval.ApiService;

[Workflow]
public interface IMyWorkflow
{
    [WorkflowRun]
    Task<string> RunAsync();
}