namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents the current state of the Truco game
    /// </summary>
    public class GameStateDto
    {
        /// <summary>
        /// All players in the game
        /// </summary>
        public List<PlayerDto> Players { get; set; } = new List<PlayerDto>();

        /// <summary>
        /// Cards played in the current round (one per seat)
        /// </summary>
        public List<PlayedCardDto> PlayedCards { get; set; } = new List<PlayedCardDto>();

        /// <summary>
        /// Current points at stake in the round
        /// </summary>
        public int Stakes { get; set; }

        /// <summary>
        /// Whether Truco has been called in the current round
        /// </summary>
        public bool IsTrucoCalled { get; set; }

        /// <summary>
        /// Whether raising the stakes is currently allowed
        /// </summary>
        public bool IsRaiseEnabled { get; set; }

        /// <summary>
        /// The current hand number in the match
        /// </summary>
        public int CurrentHand { get; set; }

        /// <summary>
        /// The scores for each team (team names as keys, scores as values)
        /// </summary>
        public Dictionary<string, int> TeamScores { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// The team that won the current turn, or null if undecided
        /// </summary>
        public string? TurnWinner { get; set; }

        /// <summary>
        /// Log of actions that have occurred in the game
        /// </summary>
        public List<ActionLogEntryDto> ActionLog { get; set; } = new List<ActionLogEntryDto>();
    }
}
