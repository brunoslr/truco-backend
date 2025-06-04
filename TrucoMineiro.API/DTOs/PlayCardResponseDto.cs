namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Response for the play card endpoint
    /// </summary>
    public class PlayCardResponseDto
    {
        /// <summary>
        /// The current game state after the card was played
        /// </summary>
        public GameStateDto GameState { get; set; } = new();

        /// <summary>
        /// The requesting player's current hand
        /// </summary>
        public List<CardDto> Hand { get; set; } = new();

        /// <summary>
        /// All player hands with appropriate card visibility
        /// </summary>
        public List<PlayerHandDto> PlayerHands { get; set; } = new();

        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result of the action
        /// </summary>
        public string Message { get; set; } = string.Empty;
    }
}
