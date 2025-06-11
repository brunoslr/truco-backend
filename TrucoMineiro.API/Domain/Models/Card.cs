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
        /// Creates a fold card (represents a player folding)
        /// </summary>
        public static Card CreateFoldCard()
        {
            return new Card(TrucoConstants.Cards.FoldValue, TrucoConstants.Cards.FoldSuit);
        }

        /// <summary>
        /// Creates a Empty card (card placeholder before it is chosen)
        /// </summary>
        public static Card CreateEmptyCard()
        {
            return new Card(TrucoConstants.Cards.EmptyValue, TrucoConstants.Cards.EmptySuit);
        }        /// <summary>
        /// Checks if this card represents a fold
        /// </summary>
        public bool IsFold => Value == TrucoConstants.Cards.FoldValue && Suit == TrucoConstants.Cards.FoldSuit;

        /// <summary>
        /// Checks if this card represents an empty slot
        /// </summary>
        public bool IsEmpty => Value == TrucoConstants.Cards.EmptyValue && Suit == TrucoConstants.Cards.EmptySuit;

        /// <summary>
        /// Returns a string representation of the card
        /// </summary>
        public override string ToString() => IsFold ? "FOLD" : IsEmpty ? "EMPTY" : $"{Value} of {Suit}";
    }
}
