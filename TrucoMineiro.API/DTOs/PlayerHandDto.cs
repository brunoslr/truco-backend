namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents a player's hand with potential hidden cards
    /// </summary>
    public class PlayerHandDto
    {
        /// <summary>
        /// The player's seat number
        /// </summary>
        public int Seat { get; set; }

        /// <summary>
        /// Cards in the player's hand
        /// When hidden, these will be empty CardDto objects with no value/suit
        /// </summary>
        public List<CardDto> Cards { get; set; } = new List<CardDto>();
    }
}
