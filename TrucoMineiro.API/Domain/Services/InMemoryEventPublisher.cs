using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// In-memory implementation of event publisher for single-instance scenarios.
    /// Publishes events to all registered handlers using dependency injection.
    /// </summary>
    public class InMemoryEventPublisher : IEventPublisher
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<InMemoryEventPublisher> _logger;

        public InMemoryEventPublisher(IServiceScopeFactory serviceScopeFactory, ILogger<InMemoryEventPublisher> logger)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <summary>
        /// Publishes an event to all registered handlers
        /// </summary>
        public async Task PublishAsync<T>(T gameEvent) where T : IGameEvent
        {
            await PublishAsync(gameEvent, CancellationToken.None);
        }        /// <summary>
        /// Publishes an event to all registered handlers with cancellation support
        /// </summary>
        public async Task PublishAsync<T>(T gameEvent, CancellationToken cancellationToken = default) where T : IGameEvent
        {
            if (gameEvent == null)
            {
                _logger.LogWarning("Attempted to publish null event");
                return;
            }            
            try
            {
                // Create a scope to resolve scoped services
                using var scope = _serviceScopeFactory.CreateScope();
                
                // Get all handlers for this event type
                var handlers = scope.ServiceProvider.GetServices<IEventHandler<T>>().ToList();                if (!handlers.Any())
                {
                    return;
                }

                // Sort handlers by priority (lower numbers = higher priority)
                var sortedHandlers = handlers
                    .Where(h => h.CanHandle(gameEvent))
                    .OrderBy(h => h.Priority)
                    .ToList();

                _logger.LogDebug("Found {HandlerCount} handlers for event {EventType}", sortedHandlers.Count, gameEvent.EventType);

                // Execute handlers in priority order
                foreach (var handler in sortedHandlers)
                {
                    try
                    {
                        await handler.HandleAsync(gameEvent, cancellationToken);
                        _logger.LogDebug("Handler {HandlerType} processed event {EventType} successfully", 
                            handler.GetType().Name, gameEvent.EventType);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Handler {HandlerType} failed to process event {EventType} for game {GameId}", 
                            handler.GetType().Name, gameEvent.EventType, gameEvent.GameId);
                        
                        // Continue processing other handlers even if one fails
                        // In production, you might want to implement retry logic or dead letter queues
                    }
                }

                _logger.LogDebug("Completed publishing event {EventType} for game {GameId}", 
                    gameEvent.EventType, gameEvent.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish event {EventType} for game {GameId}", 
                    gameEvent.EventType, gameEvent.GameId);
                throw;
            }
        }        /// <summary>
        /// Gets the number of registered handlers for a specific event type
        /// </summary>
        public int GetHandlerCount<T>() where T : IGameEvent
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                var handlers = scope.ServiceProvider.GetServices<IEventHandler<T>>();
                return handlers.Count();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get handler count for event type {EventType}", typeof(T).Name);
                return 0;
            }
        }
    }
}
