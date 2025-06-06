namespace TrucoMineiro.API.Domain.Events
{
    /// <summary>
    /// Base interface for all game events in the Truco system.
    /// Events represent things that have happened in the game and cannot be changed.
    /// </summary>
    public interface IGameEvent
    {
        /// <summary>
        /// Unique identifier of the game this event belongs to
        /// </summary>
        Guid GameId { get; }

        /// <summary>
        /// Unique identifier for this specific event
        /// </summary>
        Guid EventId { get; }

        /// <summary>
        /// When this event occurred (UTC)
        /// </summary>
        DateTime OccurredAt { get; }

        /// <summary>
        /// The type/name of this event (used for serialization and routing)
        /// </summary>
        string EventType { get; }

        /// <summary>
        /// Version of the event schema (for future compatibility)
        /// </summary>
        int Version { get; }

        /// <summary>
        /// ID of the player who triggered this event (if applicable)
        /// </summary>
        Guid? PlayerId { get; }
    }
}
