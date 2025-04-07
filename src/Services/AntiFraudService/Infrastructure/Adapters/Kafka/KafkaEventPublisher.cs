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
    private readonly KafkaProducerConfig _config;
    private readonly ILogger<KafkaEventPublisher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="KafkaEventPublisher"/> class.
    /// </summary>
    /// <param name="config">Kafka producer configuration.</param>
    /// <param name="logger">Logger instance.</param>
    public KafkaEventPublisher(
        IOptions<KafkaProducerConfig> config,
        ILogger<KafkaEventPublisher> logger)
    {
        _config = config.Value;
        _logger = logger;

        var kafkaConfig = _config.ToConfluentConfig();
        _producer = new ProducerBuilder<string, string>(kafkaConfig).Build();
    }

    /// <inheritdoc/>
    public async Task PublishAsync<TEvent>(TEvent domainEvent, CancellationToken cancellationToken = default) 
        where TEvent : DomainEvent
    {
        try
        {
            // Determine topic based on event type
            string topicName = DetermineTopicName<TEvent>();
            
            // Serialize the domain event to JSON
            string eventJson = JsonSerializer.Serialize(domainEvent);
            
            // Use the event ID as the message key for partitioning
            var message = new Message<string, string>
            {
                Key = domainEvent.EventId.ToString(),
                Value = eventJson
            };

            _logger.LogInformation(
                "Publishing {EventType} to Kafka topic {TopicName} with ID {EventId}", 
                domainEvent.EventType, 
                topicName, 
                domainEvent.EventId);

            // Publish the message to Kafka
            await _producer.ProduceAsync(topicName, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing {EventType} to Kafka", domainEvent.EventType);
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
        return $"{_config.TopicPrefix}.{kebabCaseEventName}";
    }

    private static string ToKebabCase(string value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return string.Concat(
            value.Select((x, i) => i > 0 && char.IsUpper(x) ? $"-{x}" : x.ToString())
        ).ToLower();
    }
} 