using AntiFraudService.Domain.Models;

namespace AntiFraudService.Domain.Ports.Input;

public interface ICheckTransactionUseCase
{
    Task ExecuteAsync(Transaction transaction);
} 