using System;

namespace AntiFraudService.Domain.Models;

/// <summary>
/// Representa el monto total acumulado de transacciones para una cuenta en un día específico.
/// </summary>
public class DailyAccumulatedTransaction
{
    /// <summary>
    /// Identificador de la cuenta (parte de la clave compuesta).
    /// </summary>
    public Guid AccountId { get; private set; }

    /// <summary>
    /// Fecha para la cual se registra el acumulado (parte de la clave compuesta).
    /// </summary>
    public DateOnly Date { get; private set; }

    /// <summary>
    /// Monto total acumulado para la cuenta en la fecha especificada.
    /// </summary>
    public decimal AccumulatedAmount { get; private set; }

    /// <summary>
    /// Constructor privado para EF Core o creación interna.
    /// </summary>
    private DailyAccumulatedTransaction(Guid accountId, DateOnly date, decimal accumulatedAmount)
    {
        AccountId = accountId;
        Date = date;
        AccumulatedAmount = accumulatedAmount;
    }

    /// <summary>
    /// Crea una nueva instancia para un día y cuenta.
    /// </summary>
    public static DailyAccumulatedTransaction CreateNew(Guid accountId, DateOnly date, decimal initialAmount)
    {
        // Podríamos validar que initialAmount no sea negativo
        return new DailyAccumulatedTransaction(accountId, date, initialAmount);
    }

    /// <summary>
    /// Añade un monto al acumulado existente.
    /// </summary>
    public void AddAmount(decimal amountToAdd)
    {
        // Podríamos validar que amountToAdd no sea negativo
        AccumulatedAmount += amountToAdd;
        // Aquí no generamos eventos de dominio, ya que es una entidad de soporte/infraestructura
    }
} 