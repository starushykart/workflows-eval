using MassTransit;
using WorkflowEval.ApiService.Saga.Activities;
using WorkflowEval.ApiService.Saga.Events;

namespace WorkflowEval.ApiService.Saga;

public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State Submitted { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Failed { get; private set; } = null!;

    public Event<OrderSubmitted> OrderSubmitted { get; private set; } = null!;
    public Event<OrderCompleted> OrderCompleted { get; private set; } = null!;
    public Event<OrderProgress> OrderProgress { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);
        
        Event(() => OrderSubmitted, e => e.CorrelateById(x => x.Message.OrderId));
        Event(() => OrderCompleted, e => e.CorrelateById(x => x.Message.OrderId));
        Event(() => OrderProgress, e => e.CorrelateById(x => x.Message.OrderId));
        
        Initially(
            // Event is consumed, new instance is created in Initial state
            When(OrderSubmitted)
                // copy some data from the event to the saga
                .Then(context => context.Saga.SubmittedAt = context.SentTime ?? DateTime.UtcNow)
                // transition to the Submitted state
                .TransitionTo(Submitted)
        );
        
        During(Submitted,
            When(OrderCompleted)
                .Then(context => context.Saga.CompletedAt = context.SentTime ?? DateTime.UtcNow)
                .TransitionTo(Completed),
            When(OrderProgress)
                // execute activity when event occured
                .Activity(x=>x.OfType<SetProgressActivity>())
                // if progress == 100 - finalize saga
                .If(x => x.Message.Value == 100, x => x
                    .Finalize())
                // if exception occured - start compensation chain
                .Catch<Exception>(x => x
                    .TransitionTo(Failed)
                    .Finalize())
        );
        
        During(Completed, Ignore(OrderProgress));
    }
}