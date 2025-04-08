using AntiFraudService.Domain.Models;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Domain.Ports.Output; // Para Repositorio y Publisher
using Microsoft.Extensions.Logging; // Para Logging
using System;
using System.Threading.Tasks;
using AntiFraudService.Domain.Events; // Necesario para el evento

namespace AntiFraudService.Application.UseCases;

public class CheckTransactionUseCase : ICheckTransactionUseCase
{
    private readonly IDailyAccumulatedTransactionRepository _repository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<CheckTransactionUseCase> _logger;
    private const decimal DAILY_ACCOUNT_LIMIT = 20000m;

    // Inyectamos las dependencias necesarias
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



            //DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            //var accumulated = await _repository.GetByAccountAndDateAsync(transaction.SourceAccountId, today);
            //decimal currentAccumulatedAmount = accumulated?.AccumulatedAmount ?? 0m;
            //decimal potentialNewAmount = currentAccumulatedAmount + transaction.Amount.Value;

            //if (potentialNewAmount > DAILY_ACCOUNT_LIMIT)
            //{
            //    transaction.Reject(); // Mark as rejected for daily limit
            //    _logger.LogWarning("Transaction {TransactionId} rejected for exceeding daily accumulated limit (Potential: {PotentialAmount}, Limit: {Limit}).",
            //        transaction.Id, potentialNewAmount, DAILY_ACCOUNT_LIMIT);
            //}
            //else
            //{
            //    // If it does not exceed the daily limit, approve and update accumulated
            //    transaction.Approve(); // Mark as approved
            //    _logger.LogInformation("Transaction {TransactionId} approved.", transaction.Id);

            //    if (accumulated == null)
            //    {
            //        accumulated = DailyAccumulatedTransaction.CreateNew(transaction.SourceAccountId, today, transaction.Amount.Value);
            //    }
            //    else
            //    {
            //        accumulated.AddAmount(transaction.Amount.Value);
            //    }
            //    await _repository.SaveAsync(accumulated);
            //    _logger.LogInformation("Daily accumulated for account {AccountId} updated to {NewAmount}", accumulated.AccountId, accumulated.AccumulatedAmount);
            //}

            //// Publish Final Event (Approved or Rejected for daily limit)
            //var finalEvent = new TransactionValidationResultEvent(transaction.Id, transaction.Status);
            //await _eventPublisher.PublishAsync(finalEvent);
            //_logger.LogInformation("Validation result event published for transaction {TransactionId} with status {FinalStatus}", finalEvent.TransactionId, finalEvent.FinalStatus);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during transaction validation {TransactionId}", transaction.Id);
            // Consider publishing a failure event or re-throwing
        }
    }
} 