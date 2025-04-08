using AntiFraudService.Domain.Models;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Domain.Ports.Output;
using Microsoft.Extensions.Logging;

namespace AntiFraudService.Application.UseCases;

public class CheckTransactionUseCase : ICheckTransactionUseCase
{
    private readonly IDailyAccumulatedTransactionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CheckTransactionUseCase> _logger;

    public CheckTransactionUseCase(
        IDailyAccumulatedTransactionRepository repository,
        IEventPublisher eventPublisher,
        ILogger<CheckTransactionUseCase> logger)
    {
        _repository = repository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task ExecuteAsync(Transaction transaction)
    {
        _logger.LogInformation("Validating transaction {TransactionId}", transaction.Id);

        try
        {
            // 1. Validate individual limit
            transaction.ValidateAmountLimit();

            if (transaction.IsRejected) // Updates internal state if > 2000
            {
                _logger.LogWarning("Transaction {TransactionId} rejected for exceeding individual limit.", transaction.Id);

                await _eventPublisher.PublishAsync(transaction.DomainEvents);
                _logger.LogInformation("Validation result event (Rejected for Individual Limit) published for transaction {TransactionId}", transaction.Id);
                return; // Exit method
            }

            //// If we reach here, the transaction passed the individual limit.

            // 2. Validate daily accumulated limit
            var accumulated = await _repository.GetForAccountTodayAsync(transaction.SourceAccountId);

            if (accumulated == null)
            {
                accumulated = DailyAccumulatedTransaction.CreateNew(transaction.SourceAccountId, transaction.Amount);
            }else{
                accumulated.AddAmount(transaction.Amount.Value); // Update accumulated amount     
            }
            
            transaction.ValidateDailyAccountLimit(accumulated.AccumulatedAmount);
            await _repository.SaveAsync(accumulated);
            _logger.LogInformation("Transaction {TransactionId} approved and new daily accumulated created.", transaction.Id);
            await _eventPublisher.PublishAsync(transaction.DomainEvents);
            _logger.LogInformation("Validation result event (Approved) published for transaction {TransactionId}", transaction.Id);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transaction validation {TransactionId}", transaction.Id);
            // Consider publishing a failure event or re-throwing
        }
    }
} 