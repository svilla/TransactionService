using System;
using System.Threading;
using System.Threading.Tasks;

namespace AntiFraudService.Domain.Ports.Input;

/// <summary>
/// Interface for consuming messages from a message broker.
/// </summary>
/// <typeparam name="TMessage">The type of messages to consume.</typeparam>
public interface IConsumer<TMessage> where TMessage : class
{
    /// <summary>
    /// Starts consuming messages from the configured topic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token to stop consumption.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ConsumeAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the name of the topic being consumed.
    /// </summary>
    string TopicName { get; }
} 