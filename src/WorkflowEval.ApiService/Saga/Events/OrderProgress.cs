namespace WorkflowEval.ApiService.Saga.Events;

public record OrderProgress(Guid OrderId, int Value);