using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Infrastructure.Adapters.Kafka.Config;
using AntiFraudService.Infrastructure.Adapters.Kafka.DTOs;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;
using AntiFraudService.Domain.Models;

namespace AntiFraudService.Infrastructure.Adapters.Kafka;

public class KafkaConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly string _topicName;
    private bool _isCancelled;

    public KafkaConsumer(
        IServiceScopeFactory serviceScopeFactory,
        IOptions<KafkaConsumerConfig> config,
        ILogger<KafkaConsumer> logger)
    {
        _logger = logger;
        _topicName = config.Value.TopicName;
        _serviceScopeFactory = serviceScopeFactory;

        var consumerConfig = config.Value.ToConfluentConfig();
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    public string TopicName => _topicName;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka consumer starting");
        Task.Run(() => ConsumeAsync(stoppingToken), stoppingToken);
        return Task.CompletedTask;
    }

    private async Task ConsumeAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting consumption from topic {TopicName}", _topicName);

        _consumer.Subscribe(_topicName);
        _isCancelled = false;

        try
        {
            while (!_isCancelled && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var consumeResult = _consumer.Consume(TimeSpan.FromSeconds(1));

                    if (consumeResult != null)
                    {
                        _logger.LogDebug("Message received: {Message}", consumeResult.Message.Value);

                        using (var scope = _serviceScopeFactory.CreateScope())
                        {
                            var checkTransactionUseCase = scope.ServiceProvider.GetRequiredService<ICheckTransactionUseCase>();

                            try
                            {
                                var transactionCreated = JsonSerializer.Deserialize<TransactionCreated>(consumeResult.Message.Value);

                                if (transactionCreated != null)
                                {
                                    await checkTransactionUseCase.ExecuteAsync(transactionCreated.ToTransaction());
                                    _logger.LogInformation("Message processed successfully: {EventId}", transactionCreated.EventId);
                                }
                            }
                            catch (JsonException ex)
                            {
                                _logger.LogError(ex, "Error deserializing message");
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Error processing message");
                            }
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message: {Reason}", ex.Error.Reason);
                    if (ex.Error.IsFatal)
                    {
                        _logger.LogCritical("Fatal Kafka error encountered. Stopping consumer.");
                        _isCancelled = true;
                    }
                    await Task.Delay(5000, cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumption cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Unhandled exception during message consumption loop. Stopping consumer.");
            _isCancelled = true;
        }
        finally
        {
            _logger.LogInformation("Closing Kafka consumer for topic {TopicName}.", _topicName);
            try
            {
                _consumer.Close();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error closing consumer");
            }
        }
    }

    public override void Dispose()
    {
        _logger.LogInformation("Disposing Kafka consumer.");
        if (!_isCancelled)
        {
            _isCancelled = true;
        }
        _consumer?.Dispose();
        base.Dispose();
    }
}