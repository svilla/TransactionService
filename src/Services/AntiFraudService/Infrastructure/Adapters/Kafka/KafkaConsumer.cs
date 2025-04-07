using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AntiFraudService.Domain.Ports.Input;
using AntiFraudService.Infrastructure.Adapters.Kafka.Config;
using AntiFraudService.Infrastructure.Adapters.Kafka.DTOs;
using Confluent.Kafka;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AntiFraudService.Infrastructure.Adapters.Kafka;

public class KafkaConsumer : BackgroundService, IConsumer<TransactionCreated>, IDisposable
{
    private readonly ICheckTransactionUseCase _checkTransactionUseCase;
    private readonly IConsumer<string, string> _consumer;
    private readonly ILogger<KafkaConsumer> _logger;
    private readonly string _topicName;
    private bool _isCancelled;

    public KafkaConsumer(
        ICheckTransactionUseCase checkTransactionUseCase,
        IOptions<KafkaConsumerConfig> config,
        ILogger<KafkaConsumer> logger)
    {
        _logger = logger;
        _topicName = config.Value.TopicName;
        _checkTransactionUseCase = checkTransactionUseCase;
        
        var consumerConfig = config.Value.ToConfluentConfig();
        _consumer = new ConsumerBuilder<string, string>(consumerConfig).Build();
    }

    public string TopicName => _topicName;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Kafka consumer starting");
        return ConsumeAsync(stoppingToken);
    }

    public async Task ConsumeAsync(CancellationToken cancellationToken = default)
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
                    var consumeResult = _consumer.Consume(cancellationToken);
                    
                    if (consumeResult != null)
                    {
                        _logger.LogDebug("Message received: {Message}", consumeResult.Message.Value);
                        
                        try
                        {
                            var transactionCreated = JsonSerializer.Deserialize<TransactionCreated>(consumeResult.Message.Value);
                            
                            if (transactionCreated != null)
                            {
                                await _checkTransactionUseCase.ExecuteAsync(transactionCreated.ToTransaction());
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
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Error consuming message");
                }
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Consumption cancelled");
        }
        finally
        {
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
        _isCancelled = true;
        _consumer?.Dispose();
        base.Dispose();
    }
}