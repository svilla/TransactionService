using AntiFraudService.Application.UseCases;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Domain.Ports.Output;
using AntiFraudService.Infrastructure.Adapters.Kafka;
using AntiFraudService.Infrastructure.Adapters.Kafka.Config;
using AntiFraudService.Infrastructure.Adapters.Kafka.DTOs;
using AntiFraudService.Infrastructure.Persistence;
using AntiFraudService.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Obtener la configuración
        var configuration = hostContext.Configuration;

        // Configuración de Kafka
        services.Configure<KafkaConsumerConfig>(configuration.GetSection("Kafka:Consumer"));
        services.Configure<KafkaProducerConfig>(configuration.GetSection("Kafka:Producer"));

        // Configuración de Entity Framework Core para PostgreSQL
        services.AddDbContext<AntiFraudDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AntiFraudDatabase"),
            // Opcional: Configurar estrategia de reintento para resiliencia
            npgsqlOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            }));

        // Registrar Repositorio
        services.AddScoped<IDailyAccumulatedTransactionRepository, DailyAccumulatedTransactionRepository>();

        // Registrar Publicador de Eventos
        // Usamos Singleton si el productor de Kafka es thread-safe (que lo es)
        // y si no tiene dependencias Scoped.
        services.AddSingleton<IEventPublisher, KafkaEventPublisher>();

        // Registrar puertos de entrada (casos de uso)
        services.AddScoped<ICheckTransactionUseCase, CheckTransactionUseCase>();

        // Registrar adaptadores de infraestructura
        services.AddHostedService<KafkaConsumer>();
    });

var host = builder.Build();
host.Run(); 