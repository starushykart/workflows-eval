using MassTransit;

namespace WorkflowEval.ApiService.Saga;

public class OrderState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = null!;
    
    public int Progress { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    public uint Version { get; set; }
}