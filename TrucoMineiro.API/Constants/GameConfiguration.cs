namespace TrucoMineiro.API.Constants
{
    /// <summary>
    /// Configuration constants for game initialization and rules
    /// </summary>
    public static class GameConfiguration
    {
        /// <summary>
        /// Initial dealer seat for the first hand of a new game.
        /// In Truco Mineiro, the dealer is known as the "Pé" (foot in Portuguese).
        /// Set to seat 3 (AI 3) so that the human player (seat 0) plays first.
        /// The first player is always the one to the left of the dealer.
        /// 
        /// Seat arrangement:
        /// - Seat 0: Human player
        /// - Seat 1: AI 1  
        /// - Seat 2: AI 2
        /// - Seat 3: AI 3 (Initial dealer/Pé)
        /// </summary>
        public const int InitialDealerSeat = 3;        
        
        /// <summary>
        /// Number of players in the game
        /// </summary>
        public const int MaxPlayers = 4;

        /// <summary>
        /// Minimum delay in milliseconds between AI player actions if not set in configuration
        /// </summary>
        public const int DefaultMinAIPlayDelayMs = 500;
        
        /// <summary>
        /// Maximum delay in milliseconds between AI player actions if not set in configuration
        /// </summary>
        public const int DefaultMaxAIPlayDelayMs = 2000;
          /// <summary>
        /// Default delay in milliseconds after a hand was resolved before starting next hand if not set in configuration
        /// </summary>
        public const int DefaultHandResolutionDelayMs = 5000;

        /// <summary>
        /// Default delay in milliseconds after a round was resolved before starting next round if not set in configuration
        /// </summary>
        public const int DefaultRoundResolutionDelayMs = 2000;

        /// <summary>
        /// Gets the first player seat based on the dealer seat.
        /// The first player is always to the left of the dealer (next seat clockwise).
        /// </summary>
        /// <param name="dealerSeat">The current dealer's seat</param>
        /// <returns>The seat of the first player for the hand</returns>
        public static int GetFirstPlayerSeat(int dealerSeat)
        {
            return (dealerSeat + 1) % MaxPlayers;
        }

        /// <summary>
        /// Gets the next dealer seat for the following hand.
        /// The dealer rotates clockwise (to the left) after each hand.
        /// </summary>
        /// <param name="currentDealerSeat">The current dealer's seat</param>
        /// <returns>The seat of the next dealer</returns>
        public static int GetNextDealerSeat(int currentDealerSeat)
        {
            return (currentDealerSeat + 1) % MaxPlayers;
        }
    }
}
