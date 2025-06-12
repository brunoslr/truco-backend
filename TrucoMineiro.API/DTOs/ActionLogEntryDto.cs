using System.Text.Json.Serialization;

namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents an entry in the game action log
    /// Optimized to exclude null/unused fields from JSON serialization for reduced payload size
    /// </summary>
    public class ActionLogEntryDto
    {
        /// <summary>
        /// The type of action (e.g., "card-played", "button-pressed", "hand-result", "turn-result")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The seat number of the player who performed the action (optional, depending on type)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PlayerSeat { get; set; }

        /// <summary>
        /// The card that was played (optional, for "card-played" type)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Card { get; set; }

        /// <summary>
        /// The action that was performed (optional, for "button-pressed" type)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Action { get; set; }

        /// <summary>
        /// The hand number (optional, for "hand-result" type)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? HandNumber { get; set; }

        /// <summary>
        /// The winner (optional, for "hand-result" or "turn-result" type)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? Winner { get; set; }

        /// <summary>
        /// The winning team (optional, for "turn-result" type)
        /// </summary>
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? WinnerTeam { get; set; }
    }
}
