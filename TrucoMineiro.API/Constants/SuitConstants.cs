namespace TrucoMineiro.API.Constants
{
    /// <summary>
    /// Constants for card suits with standardization and validation support
    /// </summary>
    public static class SuitConstants
    {
        // Standard Unicode suit symbols (preferred format)
        public const string Hearts = "♥";
        public const string Diamonds = "♦";
        public const string Clubs = "♣";
        public const string Spades = "♠";

        /// <summary>
        /// All valid suit symbols in the standard format
        /// </summary>
        public static readonly string[] StandardSuits = { Hearts, Diamonds, Clubs, Spades };

        /// <summary>
        /// Mapping from alternative suit names to standard symbols
        /// </summary>
        private static readonly Dictionary<string, string> SuitMappings = new(StringComparer.OrdinalIgnoreCase)
        {
            // Unicode symbols (already standard)
            { "♥", Hearts },
            { "♦", Diamonds },
            { "♣", Clubs },
            { "♠", Spades },
            
            // Full English names
            { "Hearts", Hearts },
            { "Diamonds", Diamonds },
            { "Clubs", Clubs },
            { "Spades", Spades },
            
            // Common abbreviations
            { "H", Hearts },
            { "D", Diamonds },
            { "C", Clubs },
            { "S", Spades },
            
            // Portuguese names (for Brazilian Truco)
            { "Copas", Hearts },
            { "Ouros", Diamonds },
            { "Paus", Clubs },
            { "Espadas", Spades }
        };

        /// <summary>
        /// Normalizes a suit string to the standard Unicode format
        /// </summary>
        /// <param name="suit">The suit string to normalize</param>
        /// <returns>The normalized suit symbol, or the original if not found</returns>
        public static string NormalizeSuit(string suit)
        {
            if (string.IsNullOrWhiteSpace(suit))
                return suit;

            return SuitMappings.TryGetValue(suit.Trim(), out var normalized) ? normalized : suit;
        }

        /// <summary>
        /// Checks if a suit string is valid (can be normalized)
        /// </summary>
        /// <param name="suit">The suit string to validate</param>
        /// <returns>True if the suit is valid, false otherwise</returns>
        public static bool IsValidSuit(string suit)
        {
            if (string.IsNullOrWhiteSpace(suit))
                return false;

            return SuitMappings.ContainsKey(suit.Trim());
        }

        /// <summary>
        /// Gets the full English name for a suit symbol
        /// </summary>
        /// <param name="suit">The suit symbol</param>
        /// <returns>The full English name</returns>
        public static string GetSuitName(string suit)
        {
            var normalized = NormalizeSuit(suit);
            return normalized switch
            {
                Hearts => "Hearts",
                Diamonds => "Diamonds",
                Clubs => "Clubs",
                Spades => "Spades",
                _ => suit
            };
        }
    }
}
