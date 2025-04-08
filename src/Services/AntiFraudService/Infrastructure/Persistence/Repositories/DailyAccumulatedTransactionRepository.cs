using AntiFraudService.Domain.Models;
using AntiFraudService.Domain.Ports.Output;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace AntiFraudService.Infrastructure.Persistence.Repositories;

public class DailyAccumulatedTransactionRepository : IDailyAccumulatedTransactionRepository
{
    private readonly AntiFraudDbContext _context;

    public DailyAccumulatedTransactionRepository(AntiFraudDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<DailyAccumulatedTransaction?> GetByAccountAndDateAsync(Guid accountId, DateOnly date)
    {
        // Usamos FindAsync para buscar por clave primaria compuesta
        return await _context.DailyAccumulatedTransactions
                             .FindAsync(accountId, date);
    }

    public async Task SaveAsync(DailyAccumulatedTransaction accumulatedTransaction)
    {
        // Intentamos encontrar la entidad existente por su clave primaria
        var existingEntity = await _context.DailyAccumulatedTransactions
                                           .FindAsync(accumulatedTransaction.AccountId, accumulatedTransaction.Date);

        if (existingEntity == null)
        {
            // Si no existe, la añadimos al contexto ANTES de SaveChangesAsync
            _context.DailyAccumulatedTransactions.Add(accumulatedTransaction);
        }
        else
        {
            // Si existe, actualizamos sus valores. EF Core detectará los cambios
            // en la entidad 'existingEntity' que ya está rastreando.
            // Usamos SetValues para copiar las propiedades del objeto entrante al objeto rastreado.
             _context.Entry(existingEntity).CurrentValues.SetValues(accumulatedTransaction);
        }

        // Guardamos los cambios pendientes (sean Add o Update)
        await _context.SaveChangesAsync();
    }
} 