using System.Text.Json.Serialization;
using AntiFraudService.Domain.Models;

namespace AntiFraudService.Infrastructure.Adapters.Kafka.DTOs;

/// <summary>
/// DTO that represents a transaction created event received from Kafka
/// </summary>
public class TransactionCreated
{
    /// <summary>
    /// The event's unique identifier
    /// </summary>
    [JsonPropertyName("eventId")]
    public Guid EventId { get; set; }

    /// <summary>
    /// When the event occurred
    /// </summary>
    [JsonPropertyName("occurredOn")]
    public DateTime OccurredOn { get; set; }

    /// <summary>
    /// Type of the event
    /// </summary>
    [JsonPropertyName("eventType")]
    public string EventType { get; set; } = "TransactionCreated";

    /// <summary>
    /// The source account ID
    /// </summary>
    [JsonPropertyName("sourceAccountId")]
    public Guid SourceAccountId { get; set; }

    /// <summary>
    /// The target account ID
    /// </summary>
    [JsonPropertyName("targetAccountId")]
    public Guid TargetAccountId { get; set; }

    /// <summary>
    /// The transfer type ID
    /// </summary>
    [JsonPropertyName("transferTypeId")]
    public int TransferTypeId { get; set; }

    /// <summary>
    /// The transaction amount
    /// </summary>
    [JsonPropertyName("value")]
    public decimal Value { get; set; }

    /// <summary>
    /// Maps this DTO to a domain Transaction
    /// </summary>
    /// <returns>Domain Transaction object</returns>
    public Transaction ToTransaction()
    {
        TransactionAmount amount = Value;
        
        return Transaction.CreatePending(
            id: EventId,
            sourceAccountId: SourceAccountId,
            targetAccountId: TargetAccountId,
            transferTypeId: TransferTypeId,
            amount: amount,
            createdAt: OccurredOn
        );
    }
}