using System;

namespace AntiFraudService.Domain.Exceptions;

/// <summary>
/// Base exception for all business exceptions in the domain.
/// </summary>
public abstract class BusinessException : Exception
{
    protected BusinessException(string message) 
        : base(message)
    {
    }

    protected BusinessException(string message, Exception innerException) 
        : base(message, innerException)
    {
    }
} 