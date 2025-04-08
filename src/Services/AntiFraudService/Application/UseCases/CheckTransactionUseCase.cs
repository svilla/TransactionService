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
        _logger.LogInformation("Validando transacción {TransactionId}", transaction.Id);

        try
        {
            // 1. Validar límite individual
            transaction.ValidateAmountLimit();
            
            if(transaction.IsRejected) // Modifica estado interno si > 2000
            {
                _logger.LogWarning("Transacción {TransactionId} rechazada por exceder límite individual.", transaction.Id);
                var resultEvent = new TransactionValidationResultEvent(transaction.Id, TransactionStatus.Rejected);
                await _eventPublisher.PublishAsync(resultEvent);
                _logger.LogInformation("Evento de resultado de validación (Rechazado por Límite Individual) publicado para transacción {TransactionId}", resultEvent.TransactionId);
                return; // Salir del método
            }
            // ** Salida temprana si ya fue rechazada por límite individual **
            if (transaction.Status == TransactionStatus.Rejected)
            {
                _logger.LogWarning("Transacción {TransactionId} rechazada por exceder límite individual.", transaction.Id);
                var resultEvent = new TransactionValidationResultEvent(transaction.Id, TransactionStatus.Rejected);
                await _eventPublisher.PublishAsync(resultEvent);
                _logger.LogInformation("Evento de resultado de validación (Rechazado por Límite Individual) publicado para transacción {TransactionId}", resultEvent.TransactionId);
                return; // Salir del método
            }

            // Si llegamos aquí, la transacción pasó el límite individual.

            // 2. Validar límite acumulado diario
            DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
            var accumulated = await _repository.GetByAccountAndDateAsync(transaction.SourceAccountId, today);
            decimal currentAccumulatedAmount = accumulated?.AccumulatedAmount ?? 0m;
            decimal potentialNewAmount = currentAccumulatedAmount + transaction.Amount.Value;

            if (potentialNewAmount > DAILY_ACCOUNT_LIMIT)
            {
                transaction.Reject(); // Marcamos como rechazada por límite diario
                _logger.LogWarning("Transacción {TransactionId} rechazada por exceder límite diario acumulado (Potencial: {PotentialAmount}, Límite: {Limit}).",
                    transaction.Id, potentialNewAmount, DAILY_ACCOUNT_LIMIT);
            }
            else
            {
                // Si no excede límite diario, aprobar y actualizar acumulado
                transaction.Approve(); // Marcamos como aprobada
                _logger.LogInformation("Transacción {TransactionId} aprobada.", transaction.Id);

                if (accumulated == null)
                {
                    accumulated = DailyAccumulatedTransaction.CreateNew(transaction.SourceAccountId, today, transaction.Amount.Value);
                }
                else
                {
                    accumulated.AddAmount(transaction.Amount.Value);
                }
                await _repository.SaveAsync(accumulated);
                _logger.LogInformation("Acumulado diario para cuenta {AccountId} actualizado a {NewAmount}", accumulated.AccountId, accumulated.AccumulatedAmount);
            }

            // Publicación del Evento Final (Approved o Rejected por límite diario)
            var finalEvent = new TransactionValidationResultEvent(transaction.Id, transaction.Status);
            await _eventPublisher.PublishAsync(finalEvent);
            _logger.LogInformation("Evento de resultado de validación publicado para transacción {TransactionId} con estado {FinalStatus}", finalEvent.TransactionId, finalEvent.FinalStatus);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inesperado durante la validación de la transacción {TransactionId}", transaction.Id);
            // Considerar publicar un evento de fallo o re-lanzar
        }
    }
} 