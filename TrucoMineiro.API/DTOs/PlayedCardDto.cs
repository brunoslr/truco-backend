namespace TrucoMineiro.API.DTOs
{    /// <summary>
    /// Represents a card played by a player during the game
    /// </summary>
    public class PlayedCardDto
    {
        /// <summary>
        /// The seat number of the player who played (or will play) this card (0-3)
        /// </summary>
        public int PlayerSeat { get; set; }

        /// <summary>
        /// The card that was played, or null if not played yet
        /// </summary>
        public CardDto? Card { get; set; }
    }
}
