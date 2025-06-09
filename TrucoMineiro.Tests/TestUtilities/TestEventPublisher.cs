using TrucoMineiro.API.Domain.Events;

namespace TrucoMineiro.Tests.TestUtilities
{
    /// <summary>
    /// Test implementation of event publisher that captures events for testing
    /// </summary>
    public class TestEventPublisher : IEventPublisher
    {
        private readonly List<IGameEvent> _publishedEvents = new();
        private readonly Dictionary<Type, List<object>> _eventHandlers = new();

        /// <summary>
        /// All events that have been published
        /// </summary>
        public IReadOnlyList<IGameEvent> PublishedEvents => _publishedEvents.AsReadOnly();

        /// <summary>
        /// Publishes an event and captures it for testing
        /// </summary>
        public Task PublishAsync<T>(T gameEvent) where T : IGameEvent
        {
            return PublishAsync(gameEvent, CancellationToken.None);
        }

        /// <summary>
        /// Publishes an event with cancellation support and captures it for testing
        /// </summary>
        public Task PublishAsync<T>(T gameEvent, CancellationToken cancellationToken = default) where T : IGameEvent
        {
            if (gameEvent == null)
                return Task.CompletedTask;

            _publishedEvents.Add(gameEvent);

            // Execute registered test handlers
            if (_eventHandlers.TryGetValue(typeof(T), out var handlers))
            {
                var tasks = handlers.Cast<IEventHandler<T>>()
                    .Where(h => h.CanHandle(gameEvent))
                    .OrderBy(h => h.Priority)
                    .Select(h => h.HandleAsync(gameEvent, cancellationToken));

                return Task.WhenAll(tasks);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Gets the number of registered handlers for a specific event type
        /// </summary>
        public int GetHandlerCount<T>() where T : IGameEvent
        {
            return _eventHandlers.TryGetValue(typeof(T), out var handlers) ? handlers.Count : 0;
        }

        /// <summary>
        /// Registers a test event handler
        /// </summary>
        public void RegisterHandler<T>(IEventHandler<T> handler) where T : IGameEvent
        {
            if (!_eventHandlers.ContainsKey(typeof(T)))
                _eventHandlers[typeof(T)] = new List<object>();

            _eventHandlers[typeof(T)].Add(handler);
        }

        /// <summary>
        /// Gets all events of a specific type
        /// </summary>
        public List<T> GetEvents<T>() where T : IGameEvent
        {
            return _publishedEvents.OfType<T>().ToList();
        }

        /// <summary>
        /// Gets events for a specific game
        /// </summary>
        public List<IGameEvent> GetEventsForGame(Guid gameId)
        {
            return _publishedEvents.Where(e => e.GameId == gameId).ToList();
        }

        /// <summary>
        /// Clears all captured events and handlers
        /// </summary>
        public void Clear()
        {
            _publishedEvents.Clear();
            _eventHandlers.Clear();
        }

        /// <summary>
        /// Asserts that a specific event was published
        /// </summary>
        public void AssertEventPublished<T>(Func<T, bool>? predicate = null) where T : IGameEvent
        {
            var events = GetEvents<T>();
            if (predicate == null)
            {
                if (!events.Any())
                    throw new InvalidOperationException($"No events of type {typeof(T).Name} were published");
            }
            else
            {
                if (!events.Any(predicate))
                    throw new InvalidOperationException($"No events of type {typeof(T).Name} matching the predicate were published");
            }
        }

        /// <summary>
        /// Asserts that no events of a specific type were published
        /// </summary>
        public void AssertEventNotPublished<T>() where T : IGameEvent
        {
            var events = GetEvents<T>();
            if (events.Any())
                throw new InvalidOperationException($"{events.Count} events of type {typeof(T).Name} were published, but none were expected");
        }
    }
}
