using Temporalio.Activities;

namespace WorkflowEval.ApiService.Temporal.Activities;

public class SomeActivities(ILogger<SomeActivities> logger)
{
    [Activity]
    public async Task<string> FirstAsync()
    {
        logger.LogInformation("{Activity} activity started", nameof(FirstAsync));

        await Task.Delay(TimeSpan.FromSeconds(30));
        logger.LogInformation("{Activity} activity completed", nameof(FirstAsync));

        return "Completed";
    }
    
    [Activity]
    public async Task SecondAsync(string val)
    {
        logger.LogInformation("{Activity} activity started with {Value}", nameof(FirstAsync), val);

        await Task.Delay(TimeSpan.FromSeconds(5));
        
        logger.LogInformation("{Activity} activity completed", nameof(FirstAsync));
    }
}


