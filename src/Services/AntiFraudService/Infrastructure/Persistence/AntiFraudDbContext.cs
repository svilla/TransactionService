using AntiFraudService.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace AntiFraudService.Infrastructure.Persistence;

public class AntiFraudDbContext : DbContext
{
    public DbSet<DailyAccumulatedTransaction> DailyAccumulatedTransactions { get; set; }

    public AntiFraudDbContext(DbContextOptions<AntiFraudDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración para DailyAccumulatedTransaction usando Fluent API
        modelBuilder.Entity<DailyAccumulatedTransaction>(entity =>
        {
            // Definir la clave primaria compuesta
            entity.HasKey(dat => new { dat.AccountId, dat.Date });

            // Configurar propiedades
            entity.Property(dat => dat.AccumulatedAmount.Value)
                  .HasColumnType("decimal(18, 2)")
                  .HasColumnName("AccumulatedAmount")
                  .IsRequired();

            entity.Property(dat => dat.AccountId).IsRequired();
            entity.Property(dat => dat.Date).IsRequired();

            // Podríamos añadir un índice para búsquedas rápidas si es necesario
            // entity.HasIndex(dat => new { dat.AccountId, dat.Date }).IsUnique(); // Ya es clave primaria, así que es único
            entity.HasIndex(dat => dat.AccountId); // Índice adicional para buscar por cuenta

            // Opcional: Mapear a un nombre de tabla específico
            entity.ToTable("DailyAccumulatedTransactions");
        });
    }
}