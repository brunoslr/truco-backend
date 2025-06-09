using TrucoMineiro.API.Domain.Events;

namespace TrucoMineiro.Tests.TestUtilities
{
    /// <summary>
    /// Base class for test event handlers that capture events for testing
    /// </summary>
    public abstract class TestEventHandler<T> : IEventHandler<T> where T : IGameEvent
    {
        private readonly List<T> _handledEvents = new();

        /// <summary>
        /// All events handled by this handler
        /// </summary>
        public IReadOnlyList<T> HandledEvents => _handledEvents.AsReadOnly();

        /// <summary>
        /// Number of events handled
        /// </summary>
        public int EventCount => _handledEvents.Count;

        /// <summary>
        /// Whether this handler was called
        /// </summary>
        public bool WasCalled => _handledEvents.Any();

        /// <summary>
        /// Priority of this handler
        /// </summary>
        public virtual int Priority => 0;

        /// <summary>
        /// Handles the event and captures it for testing
        /// </summary>
        public async Task HandleAsync(T gameEvent, CancellationToken cancellationToken = default)
        {
            _handledEvents.Add(gameEvent);
            await OnHandleAsync(gameEvent, cancellationToken);
        }

        /// <summary>
        /// Override this method to implement custom handling logic
        /// </summary>
        protected virtual Task OnHandleAsync(T gameEvent, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Indicates whether this handler can process the given event
        /// </summary>
        public virtual bool CanHandle(T gameEvent) => true;

        /// <summary>
        /// Clears all captured events
        /// </summary>
        public void Clear()
        {
            _handledEvents.Clear();
        }

        /// <summary>
        /// Asserts that the handler was called
        /// </summary>
        public void AssertWasCalled()
        {
            if (!WasCalled)
                throw new InvalidOperationException($"Handler {GetType().Name} was not called");
        }

        /// <summary>
        /// Asserts that the handler was called a specific number of times
        /// </summary>
        public void AssertCallCount(int expectedCount)
        {
            if (EventCount != expectedCount)
                throw new InvalidOperationException($"Handler {GetType().Name} was called {EventCount} times, expected {expectedCount}");
        }        /// <summary>
        /// Asserts that the handler was not called
        /// </summary>
        public void AssertWasNotCalled()
        {
            if (WasCalled)
                throw new InvalidOperationException($"Handler {GetType().Name} was called {EventCount} times, expected 0");
        }    }
}
