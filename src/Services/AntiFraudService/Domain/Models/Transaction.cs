namespace AntiFraudService.Domain.Models;

public class Transaction
{
    public Guid Id { get; private set; }
    public Guid SourceAccountId { get; private set; }
    public Guid TargetAccountId { get; private set; }
    public int TransferTypeId { get; private set; }
    public TransactionAmount Amount { get; private set; }
    public TransactionStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private Transaction(Guid id, Guid sourceAccountId, Guid targetAccountId, int transferTypeId, TransactionAmount amount, TransactionStatus status, DateTime createdAt)
    {
        Id = id;
        SourceAccountId = sourceAccountId;
        TargetAccountId = targetAccountId;
        TransferTypeId = transferTypeId;
        Amount = amount;
        Status = status;
        CreatedAt = createdAt;
    }

    public static Transaction CreatePending(Guid id, Guid sourceAccountId, Guid targetAccountId, int transferTypeId, TransactionAmount amount, DateTime createdAt)
    {
        return new Transaction(id, sourceAccountId, targetAccountId, transferTypeId, amount, TransactionStatus.Pending, createdAt);
    }
} 