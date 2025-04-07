namespace AntiFraudService.Infrastructure.Adapters.Kafka.Config;

/// <summary>
/// Configuration settings for Kafka producer.
/// </summary>
public class KafkaProducerConfig
{
    /// <summary>
    /// Gets or sets the comma-separated list of Kafka broker addresses.
    /// </summary>
    public string BootstrapServers { get; set; } = "localhost:9092";

    /// <summary>
    /// Gets or sets the client identifier.
    /// </summary>
    public string ClientId { get; set; } = "antifraud-service";

    /// <summary>
    /// Gets or sets the acknowledgment level.
    /// </summary>
    public string Acks { get; set; } = "All";

    /// <summary>
    /// Gets or sets a value indicating whether to enable idempotence.
    /// </summary>
    public bool EnableIdempotence { get; set; } = true;

    /// <summary>
    /// Gets or sets the maximum number of in-flight requests.
    /// </summary>
    public int MaxInFlight { get; set; } = 5;

    /// <summary>
    /// Gets or sets the maximum number of retries.
    /// </summary>
    public int MessageSendMaxRetries { get; set; } = 3;

    /// <summary>
    /// Gets or sets the retry backoff time in milliseconds.
    /// </summary>
    public int RetryBackoffMs { get; set; } = 1000;

    /// <summary>
    /// Gets or sets the topic prefix.
    /// </summary>
    public string TopicPrefix { get; set; } = "antifraud";

    /// <summary>
    /// Converts this configuration to a Confluent.Kafka.ProducerConfig instance.
    /// </summary>
    /// <returns>A Confluent.Kafka.ProducerConfig instance.</returns>
    public Confluent.Kafka.ProducerConfig ToConfluentConfig()
    {
        return new Confluent.Kafka.ProducerConfig
        {
            BootstrapServers = BootstrapServers,
            ClientId = ClientId,
            Acks = GetAcks(),
            EnableIdempotence = EnableIdempotence,
            MaxInFlight = MaxInFlight,
            MessageSendMaxRetries = MessageSendMaxRetries,
            RetryBackoffMs = RetryBackoffMs
        };
    }

    private Confluent.Kafka.Acks GetAcks()
    {
        if (string.IsNullOrEmpty(Acks))
            return Confluent.Kafka.Acks.All;
            
        switch (Acks.ToLower())
        {
            case "all":
                return Confluent.Kafka.Acks.All;
            case "0":
            case "none":
                return Confluent.Kafka.Acks.None;
            case "leader":
                return Confluent.Kafka.Acks.Leader;
            default:
                return Confluent.Kafka.Acks.All;
        }
    }
} 