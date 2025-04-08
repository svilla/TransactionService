using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AntiFraudService.Domain.Events;
using AntiFraudService.Domain.Ports.Output;
using AntiFraudService.Infrastructure.Adapters.Kafka.Config;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AntiFraudService.Infrastructure.Adapters.Kafka;

/// <summary>
/// Implementation of IEventPublisher that publishes domain events to Kafka.
/// </summary>
public class KafkaEventPublisher : IEventPublisher
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaEventPublisher> _logger;
    private readonly string _topicPrefix;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaEventPublisher"/> class.
    /// </summary>
    /// <param name="config">Kafka producer configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public KafkaEventPublisher(
        IOptions<KafkaProducerConfig> config,
        ILogger<KafkaEventPublisher> logger)
    {
        _logger = logger;
        _topicPrefix = config.Value.TopicPrefix; // Usaremos un prefijo para los topics de salida

        var producerConfig = config.Value.ToConfluentConfig();
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    /// <inheritdoc/>
    public async Task PublishAsync(DomainEvent domainEvent)
    {
        // Determinar el topic basado en el tipo de evento (simple ejemplo)
        string topicName = $"{_topicPrefix}-validation-results"; // ej: "anti-fraud-validation-results"
        if (domainEvent is TransactionValidationResultEvent validationEvent)
        {
            // Podríamos tener topics diferentes para Approved vs Rejected si quisiéramos
        }
        else
        {
            _logger.LogWarning("Publicando evento de tipo desconocido {EventType} a topic genérico.", domainEvent.GetType().Name);
            topicName = $"{_topicPrefix}-unknown-events";
        }

        try
        {
            var messageJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType());
            var message = new Message<string, string> 
            { 
                // Usamos el ID del evento o el ID de la transacción como clave?
                // Usar TransactionId asegura que todos los eventos de una transacción vayan a la misma partición.
                Key = (domainEvent is TransactionValidationResultEvent tev ? tev.TransactionId.ToString() : domainEvent.EventId.ToString()),
                Value = messageJson 
            };

            _logger.LogInformation("Publicando evento {EventId} de tipo {EventType} a topic {TopicName}", domainEvent.EventId, domainEvent.GetType().Name, topicName);
            var deliveryResult = await _producer.ProduceAsync(topicName, message);
            _logger.LogDebug("Evento publicado a partición {Partition}, offset {Offset}", deliveryResult.Partition, deliveryResult.Offset);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Error serializando evento {EventId} de tipo {EventType}", domainEvent.EventId, domainEvent.GetType().Name);
            // Considerar qué hacer aquí (¿lanzar excepción?, ¿intentar de nuevo?, ¿loguear y continuar?)
            throw; // Re-lanzar por ahora
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Error publicando evento {EventId} a Kafka: {Reason}", domainEvent.EventId, ex.Error.Reason);
            // Considerar estrategias de reintento o dead-letter queue
            throw; // Re-lanzar por ahora
        }
        catch (Exception ex)
        {
             _logger.LogError(ex, "Error inesperado publicando evento {EventId}", domainEvent.EventId);
             throw;
        }
    }

    private string DetermineTopicName<TEvent>() where TEvent : DomainEvent
    {
        // Get the event type name without the "Event" suffix if it exists
        string eventTypeName = typeof(TEvent).Name;
        if (eventTypeName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            eventTypeName = eventTypeName.Substring(0, eventTypeName.Length - 5);
        }

        // Format: prefix.event-name (kebab-case)
        string kebabCaseEventName = ToKebabCase(eventTypeName);
        return $"{_topicPrefix}.{kebabCaseEventName}";
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return string.Concat(
            value.Select((x, i) => i > 0 && char.IsUpper(x) ? $"-{x}" : x.ToString())
        ).ToLower();
    }

    // Podríamos añadir un método Dispose para el IProducer si es necesario
    // public void Dispose() => _producer?.Dispose();
} 