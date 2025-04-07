using AntiFraudService.Application.UseCases;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Domain.Ports.Output;
using AntiFraudService.Infrastructure.Adapters.Kafka;
using AntiFraudService.Infrastructure.Adapters.Kafka.Config;
using AntiFraudService.Infrastructure.Adapters.Kafka.DTOs;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((hostContext, services) =>
    {
        // Configuración de Kafka
        services.Configure<KafkaConsumerConfig>(hostContext.Configuration.GetSection("Kafka:Consumer"));
        services.Configure<KafkaProducerConfig>(hostContext.Configuration.GetSection("Kafka:Producer"));
        
        // Registrar puertos de entrada (casos de uso)
        services.AddScoped<ICheckTransactionUseCase, CheckTransactionUseCase>();
        
        // Registrar adaptadores de infraestructura
        services.AddHostedService<KafkaConsumer>();
    });

var host = builder.Build();
host.Run(); 