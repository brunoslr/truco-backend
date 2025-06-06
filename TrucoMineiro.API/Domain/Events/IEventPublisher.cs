namespace TrucoMineiro.API.Domain.Events
{
    /// <summary>
    /// Service responsible for publishing events to registered handlers.
    /// This is the central hub for event distribution in the game system.
    /// </summary>
    public interface IEventPublisher
    {
        /// <summary>
        /// Publishes an event to all registered handlers
        /// </summary>
        /// <typeparam name="T">Type of event being published</typeparam>
        /// <param name="gameEvent">The event to publish</param>
        /// <returns>Task representing the async operation</returns>
        Task PublishAsync<T>(T gameEvent) where T : IGameEvent;

        /// <summary>
        /// Publishes an event to all registered handlers with cancellation support
        /// </summary>
        /// <typeparam name="T">Type of event being published</typeparam>
        /// <param name="gameEvent">The event to publish</param>
        /// <param name="cancellationToken">Token to cancel the operation</param>
        /// <returns>Task representing the async operation</returns>
        Task PublishAsync<T>(T gameEvent, CancellationToken cancellationToken = default) where T : IGameEvent;

        /// <summary>
        /// Gets the number of registered handlers for a specific event type
        /// </summary>
        /// <typeparam name="T">Type of event</typeparam>
        /// <returns>Number of handlers</returns>
        int GetHandlerCount<T>() where T : IGameEvent;
    }
}
