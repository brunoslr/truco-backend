using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Represents a playing card in the Truco game
    /// </summary>
    public class Card
    {
        /// <summary>
        /// The value of the card (e.g., "4", "7", "A", "K")
        /// </summary>
        public string Value { get; set; } = string.Empty;

        /// <summary>
        /// The suit of the card (e.g., "♣", "♥", "♠", "♦")
        /// </summary>
        public string Suit { get; set; } = string.Empty;

        public Card(string value, string suit)
        {
            Value = value;
            Suit = SuitConstants.NormalizeSuit(suit); // Auto-normalize suit to standard format
        }

        public Card() { }

        /// <summary>
        /// Returns a string representation of the card
        /// </summary>
        public override string ToString() => $"{Value} of {Suit}";
    }
}
