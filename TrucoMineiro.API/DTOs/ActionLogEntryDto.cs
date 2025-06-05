namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents an entry in the game action log
    /// </summary>
    public class ActionLogEntryDto
    {
        /// <summary>
        /// The type of action (e.g., "card-played", "button-pressed", "hand-result", "turn-result")
        /// </summary>
        public string Type { get; set; } = string.Empty;        /// <summary>
        /// The seat number of the player who performed the action (optional, depending on type)
        /// </summary>
        public int? PlayerSeat { get; set; }

        /// <summary>
        /// The card that was played (optional, for "card-played" type)
        /// </summary>
        public string? Card { get; set; }

        /// <summary>
        /// The action that was performed (optional, for "button-pressed" type)
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// The hand number (optional, for "hand-result" type)
        /// </summary>
        public int? HandNumber { get; set; }

        /// <summary>
        /// The winner (optional, for "hand-result" or "turn-result" type)
        /// </summary>
        public string? Winner { get; set; }

        /// <summary>
        /// The winning team (optional, for "turn-result" type)
        /// </summary>
        public string? WinnerTeam { get; set; }
    }
}
