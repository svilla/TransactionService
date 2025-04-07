using AntiFraudService.Domain.Exceptions;

namespace AntiFraudService.Domain.Models;

public readonly record struct TransactionAmount
{
    public decimal Value { get; }

    private TransactionAmount(decimal value)
    {
        if (value < 0)
        {
            throw new NegativeTransactionAmountException(value);
        }

        Value = value;
    }

    public static implicit operator TransactionAmount(decimal value)
    {
        return new TransactionAmount(value);
    }

    public static implicit operator decimal(TransactionAmount transactionAmount)
    {
        return transactionAmount.Value;
    }
} 