using System.Threading;
using System.Threading.Tasks;
using AntiFraudService.Domain.Events;

namespace AntiFraudService.Domain.Ports.Output;

/// <summary>
/// Define la operación para publicar eventos de dominio.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publica un evento de dominio.
    /// </summary>
    /// <param name="domainEvent">El evento a publicar.</param>
    Task PublishAsync(DomainEvent domainEvent);
} 