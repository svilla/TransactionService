using AntiFraudService.Domain.Models;

namespace AntiFraudService.Domain.Events;

public class TransactionValidationResultEvent : DomainEvent
{
    public Guid TransactionId { get; init; }
    public TransactionStatus FinalStatus { get; init; } // Pending, Approved, Rejected

    public TransactionValidationResultEvent(Guid transactionId, TransactionStatus finalStatus)
        : base()
    {
        TransactionId = transactionId;
        FinalStatus = finalStatus;
    }
} 