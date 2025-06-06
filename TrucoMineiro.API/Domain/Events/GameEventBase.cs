using System.Text.Json.Serialization;

namespace TrucoMineiro.API.Domain.Events
{
    /// <summary>
    /// Base implementation for all game events in the Truco system.
    /// Provides common functionality and ensures consistent event structure.
    /// </summary>
    public abstract class GameEventBase : IGameEvent
    {
        /// <summary>
        /// Unique identifier of the game this event belongs to
        /// </summary>
        public Guid GameId { get; protected set; }

        /// <summary>
        /// Unique identifier for this specific event
        /// </summary>
        public Guid EventId { get; protected set; }

        /// <summary>
        /// When this event occurred (UTC)
        /// </summary>
        public DateTime OccurredAt { get; protected set; }

        /// <summary>
        /// The type/name of this event (derived from class name)
        /// </summary>
        public virtual string EventType => GetType().Name;

        /// <summary>
        /// Version of the event schema (for future compatibility)
        /// </summary>
        public virtual int Version => 1;

        /// <summary>
        /// ID of the player who triggered this event (if applicable)
        /// </summary>
        public Guid? PlayerId { get; protected set; }

        /// <summary>
        /// Protected constructor for derived classes
        /// </summary>
        protected GameEventBase(Guid gameId, Guid? playerId = null)
        {
            GameId = gameId;
            PlayerId = playerId;
            EventId = Guid.NewGuid();
            OccurredAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Protected parameterless constructor for deserialization
        /// </summary>
        [JsonConstructor]
        protected GameEventBase()
        {
            // Used by JSON deserializer
        }

        /// <summary>
        /// Creates a correlation ID for event tracing
        /// </summary>
        public string GetCorrelationId()
        {
            return $"{GameId:N}-{EventId:N}";
        }

        /// <summary>
        /// Returns a string representation of the event for logging
        /// </summary>
        public override string ToString()
        {
            return $"{EventType}[GameId={GameId:N}, EventId={EventId:N}, OccurredAt={OccurredAt:yyyy-MM-dd HH:mm:ss}]";
        }
    }
}
