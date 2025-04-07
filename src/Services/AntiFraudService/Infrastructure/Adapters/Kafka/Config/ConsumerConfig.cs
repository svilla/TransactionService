namespace AntiFraudService.Infrastructure.Adapters.Kafka.Config;

/// <summary>
/// Configuration settings for Kafka consumer.
/// </summary>
public class KafkaConsumerConfig
{
    /// <summary>
    /// Gets or sets the comma-separated list of Kafka broker addresses.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the consumer group identifier.
    /// </summary>
    public string GroupId { get; set; } = "antifraud-service";

    /// <summary>
    /// Gets or sets the auto offset reset behavior (Earliest, Latest, Error).
    /// </summary>
    public string AutoOffsetReset { get; set; } = "Earliest";

    /// <summary>
    /// Gets or sets a value indicating whether to enable auto-commit.
    /// </summary>
    public bool EnableAutoCommit { get; set; } = true;

    /// <summary>
    /// Gets or sets the auto-commit interval in milliseconds.
    /// </summary>
    public int AutoCommitIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Gets or sets the session timeout in milliseconds.
    /// </summary>
    public int SessionTimeoutMs { get; set; } = 30000;

    /// <summary>
    /// Gets or sets the Kafka topic name to consume from.
    /// </summary>
    public string TopicName { get; set; } = "transactions";

    /// <summary>
    /// Converts this configuration to a Confluent.Kafka.ConsumerConfig instance.
    /// </summary>
    /// <returns>A Confluent.Kafka.ConsumerConfig instance.</returns>
    public Confluent.Kafka.ConsumerConfig ToConfluentConfig()
    {
        return new Confluent.Kafka.ConsumerConfig
        {
            BootstrapServers = BootstrapServers,
            GroupId = GroupId,
            AutoOffsetReset = GetAutoOffsetReset(),
            EnableAutoCommit = EnableAutoCommit,
            AutoCommitIntervalMs = AutoCommitIntervalMs,
            SessionTimeoutMs = SessionTimeoutMs
        };
    }

    private Confluent.Kafka.AutoOffsetReset GetAutoOffsetReset()
    {
        return AutoOffsetReset?.ToLower() switch
        {
            "earliest" => Confluent.Kafka.AutoOffsetReset.Earliest,
            "latest" => Confluent.Kafka.AutoOffsetReset.Latest,
            _ => Confluent.Kafka.AutoOffsetReset.Error
        };
    }
} 