using System;

namespace AntiFraudService.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to create a TransactionAmount with a negative value.
/// </summary>
public class NegativeTransactionAmountException : BusinessException
{
    public decimal AttemptedValue { get; }

    public NegativeTransactionAmountException(decimal attemptedValue)
        : base($"Cannot create a TransactionAmount with negative value: {attemptedValue}")
    {
        AttemptedValue = attemptedValue;
    }

    public NegativeTransactionAmountException(decimal attemptedValue, Exception innerException)
        : base($"Cannot create a TransactionAmount with negative value: {attemptedValue}", innerException)
    {
        AttemptedValue = attemptedValue;
    }
} 