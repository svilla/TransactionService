using System.Threading;
using System.Threading.Tasks;
using AntiFraudService.Domain.Events;
using System.Collections.Generic;

namespace AntiFraudService.Domain.Ports.Output;

/// <summary>
/// Define la operación para publicar eventos de dominio.
/// </summary>
public interface IEventPublisher
{
    /// <summary>
    /// Publica una colección de eventos de dominio.
    /// </summary>
    /// <param name="domainEvents">Los eventos a publicar.</param>
    Task PublishAsync(IEnumerable<DomainEvent> domainEvents);
} 