namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents a card played by a player during the game
    /// </summary>
    public class PlayedCardDto
    {
        /// <summary>
        /// The ID of the player who played (or will play) this card
        /// </summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>
        /// The card that was played, or null if not played yet
        /// </summary>
        public CardDto? Card { get; set; }
    }
}
