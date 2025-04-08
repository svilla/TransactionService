using AntiFraudService.Domain.Models;
using System;
using System.Threading.Tasks;

namespace AntiFraudService.Domain.Ports.Output;

/// <summary>
/// Define las operaciones para acceder a los datos de transacciones acumuladas diarias.
/// </summary>
public interface IDailyAccumulatedTransactionRepository
{
    /// <summary>
    /// Obtiene el registro acumulado para una cuenta en la fecha actual (hoy).
    /// </summary>
    /// <param name="accountId">El ID de la cuenta.</param>
    /// <returns>El registro acumulado de hoy o null si no existe.</returns>
    Task<DailyAccumulatedTransaction?> GetForAccountTodayAsync(Guid accountId);

    /// <summary>
    /// Guarda (añade o actualiza) un registro de transacción acumulada.
    /// </summary>
    /// <param name="accumulatedTransaction">El registro a guardar.</param>
    Task SaveAsync(DailyAccumulatedTransaction accumulatedTransaction);

} 