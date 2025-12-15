namespace TodoApi.Models;

public class OutboxEvent
{
    public Guid Id { get; set; }
    public Guid EventId { get; set; }  // For idempotency
    public string EventType { get; set; } = string.Empty;
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;  // JSON
    public DateTime CreatedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
}
