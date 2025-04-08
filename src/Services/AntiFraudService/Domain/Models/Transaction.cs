using AntiFraudService.Domain.Events;

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

    public bool IsRejected => Status == TransactionStatus.Rejected;

    private const decimal MAX_ALLOWED_AMOUNT = 2000m;

    private const decimal DAILY_ACCOUNT_LIMIT = 20000m;

    private readonly List<DomainEvent> _domainEvents = new();
    public IReadOnlyCollection<DomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

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

    public void ValidateAmountLimit()
    {
        if (Status == TransactionStatus.Pending && Amount.Value > MAX_ALLOWED_AMOUNT)
        {
            Reject();
        }
    }

    public void Approve()
    {
        if (Status == TransactionStatus.Pending)
        {
            Status = TransactionStatus.Approved;
            _domainEvents.Add(new TransactionValidationResultEvent(Id, TransactionStatus.Approved));
        }
    }

    private void Reject()
    {
        if (Status != TransactionStatus.Rejected)
        {
            Status = TransactionStatus.Rejected;
            _domainEvents.Add(new TransactionValidationResultEvent(Id, TransactionStatus.Rejected));
        }
    }

    public void ValidateDailyAccountLimit(TransactionAmount accumulatedAmount)
    {
        if (accumulatedAmount > DAILY_ACCOUNT_LIMIT)
        {
            Reject();
        }
        else
        {
            Approve();
        }
    }
} 