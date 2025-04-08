using System;
using System.Collections.Generic;
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
        _topicPrefix = config.Value.TopicPrefix; // We'll use a prefix for output topics

        var producerConfig = config.Value.ToConfluentConfig();
        _producer = new ProducerBuilder<string, string>(producerConfig).Build();
    }

    /// <inheritdoc/>
    public async Task PublishAsync(IEnumerable<DomainEvent> domainEvents)
    {
        if (domainEvents == null || !domainEvents.Any())
        {
            _logger.LogDebug("No domain events provided to publish.");
            return;
        }

        _logger.LogInformation("Publishing {EventCount} domain event(s)...", domainEvents.Count());

        foreach (var domainEvent in domainEvents)
        {
            // Determine topic based on event type (simple example)
            string topicName = $"{_topicPrefix}-validation-results"; // e.g., "anti-fraud-validation-results"
            if (domainEvent is TransactionValidationResultEvent validationEvent)
            {
                // We could have logic here to determine topic based on Approved/Rejected if needed
                // topicName = validationEvent.FinalStatus == Domain.Models.TransactionStatus.Approved ? ... : ... ;
            }
            else
            {
                _logger.LogWarning("Determining topic for unknown event type {EventType}.", domainEvent.GetType().Name);
                topicName = DetermineTopicName(domainEvent); // Use helper function if refined
            }

            try
            {
                // Use options to handle circular references if needed, although not expected here
                var options = new JsonSerializerOptions { ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve };
                var messageJson = JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), options);

                var message = new Message<string, string>
                {
                    // Use TransactionId if it's a result event, otherwise EventId
                    Key = (domainEvent is TransactionValidationResultEvent tev ? tev.TransactionId.ToString() : domainEvent.EventId.ToString()),
                    Value = messageJson
                };

                _logger.LogInformation("Publishing event {EventId} of type {EventType} to topic {TopicName}", domainEvent.EventId, domainEvent.GetType().Name, topicName);
                // Publish and wait for confirmation for each event
                var deliveryResult = await _producer.ProduceAsync(topicName, message);
                _logger.LogDebug("Event {EventId} published to partition {Partition}, offset {Offset}", domainEvent.EventId, deliveryResult.Partition, deliveryResult.Offset);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error serializing event {EventId} of type {EventType}. Skipping event.", domainEvent.EventId, domainEvent.GetType().Name);
                // Continue with the next event
            }
            catch (ProduceException<string, string> ex)
            {
                _logger.LogError(ex, "Error publishing event {EventId} to Kafka: {Reason}. Skipping event.", domainEvent.EventId, ex.Error.Reason);
                // Continue with the next event
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error publishing event {EventId}. Skipping event.", domainEvent.EventId);
                // Continue with the next event
            }
        } // End foreach

        _logger.LogInformation("Event publishing completed.");
    }

    // Keep the helper function to determine topic names (adapted)
    private string DetermineTopicName(DomainEvent domainEvent)
    {
        string eventTypeName = domainEvent.GetType().Name;
        if (eventTypeName.EndsWith("Event", StringComparison.OrdinalIgnoreCase))
        {
            eventTypeName = eventTypeName.Substring(0, eventTypeName.Length - 5);
        }
        string kebabCaseEventName = ToKebabCase(eventTypeName);
        // Decide whether to use a generic or specific topic
        bool isKnownType = domainEvent is TransactionValidationResultEvent; // Extend if more known types exist
        return isKnownType ? $"{_topicPrefix}.{kebabCaseEventName}" : $"{_topicPrefix}.unknown-events";
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