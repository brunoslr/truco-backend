namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents a playing card in the Truco game
    /// </summary>
    public class CardDto
    {
        /// <summary>
        /// The value of the card (e.g., "4", "7", "A", "K")
        /// Null when the card is hidden (for AI players)
        /// </summary>
        public string? Value { get; set; } = string.Empty;

        /// <summary>
        /// The suit of the card (e.g., "Clubs", "Hearts", "Spades", "Diamonds")
        /// Null when the card is hidden (for AI players)
        /// </summary>
        public string? Suit { get; set; } = string.Empty;
    }
}
