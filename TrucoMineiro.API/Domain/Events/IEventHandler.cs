namespace TrucoMineiro.API.Domain.Events
{
    /// <summary>
    /// Interface for handling specific types of game events.
    /// Implement this interface to create event handlers that respond to game events.
    /// </summary>
    /// <typeparam name="T">Type of event this handler processes</typeparam>
    public interface IEventHandler<T> where T : IGameEvent
    {
        /// <summary>
        /// Handles the specified game event
        /// </summary>
        /// <param name="gameEvent">The event to handle</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the async operation</returns>
        Task HandleAsync(T gameEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indicates whether this handler can process the given event
        /// (useful for conditional handling based on game state)
        /// </summary>
        /// <param name="gameEvent">The event to check</param>
        /// <returns>True if the handler can process this event</returns>
        bool CanHandle(T gameEvent) => true;

        /// <summary>
        /// Priority of this handler (lower numbers = higher priority)
        /// Used to control execution order when multiple handlers exist
        /// </summary>
        int Priority => 0;
    }

    /// <summary>
    /// Base class for event handlers providing common functionality
    /// </summary>
    /// <typeparam name="T">Type of event this handler processes</typeparam>
    public abstract class EventHandlerBase<T> : IEventHandler<T> where T : IGameEvent
    {
        /// <summary>
        /// Handles the specified game event
        /// </summary>
        /// <param name="gameEvent">The event to handle</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the async operation</returns>
        public abstract Task HandleAsync(T gameEvent, CancellationToken cancellationToken = default);

        /// <summary>
        /// Indicates whether this handler can process the given event
        /// Override this method to add conditional logic
        /// </summary>
        /// <param name="gameEvent">The event to check</param>
        /// <returns>True if the handler can process this event</returns>
        public virtual bool CanHandle(T gameEvent) => true;

        /// <summary>
        /// Priority of this handler (lower numbers = higher priority)
        /// Override this property to change execution order
        /// </summary>
        public virtual int Priority => 0;
    }
}
