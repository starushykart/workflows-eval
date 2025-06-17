using MassTransit;
using Microsoft.EntityFrameworkCore;
using WorkflowEval.ApiService.Persistence;
using WorkflowEval.ApiService.Saga.Events;

namespace WorkflowEval.ApiService.Saga.Activities;

public class SetProgressActivity(ILogger<SetProgressActivity> logger, AppDbContext dBcontext) : IStateMachineActivity<OrderState, OrderProgress>
{
    public async Task Execute(BehaviorContext<OrderState, OrderProgress> context, IBehavior<OrderState, OrderProgress> next)
    {
        if (context.Message.Value > 100)
            throw new Exception("Progress cannot be greater than 100");
        
        context.Saga.Progress = context.Message.Value;

        logger.LogInformation("Progress set to {Value}", context.Message.Value);

        var order = await dBcontext.Orders
            .FirstAsync(x => x.Id == context.Message.OrderId);

        order.Progress = context.Message.Value;
        
        // do not call save changes, as changes will be submitted in a transaction during saga state change
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, OrderProgress, TException> context,
        IBehavior<OrderState, OrderProgress> next) where TException : Exception
        => next.Faulted(context);
    
    public void Probe(ProbeContext context)
        => context.CreateScope("set-progress");

    public void Accept(StateMachineVisitor visitor)
        => visitor.Visit(this);
}