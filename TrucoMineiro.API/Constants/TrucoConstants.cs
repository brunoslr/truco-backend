namespace TrucoMineiro.API.Constants
{
    /// <summary>
    /// Global constants for Truco Mineiro game rules and values
    /// </summary>
    public static class TrucoConstants
    {
        /// <summary>
        /// Stakes and scoring constants
        /// </summary>
        public static class Stakes
        {
            /// <summary>
            /// Initial stakes value when a hand starts (2 points)
            /// </summary>
            public const int Initial = 2;

            /// <summary>
            /// Stakes value when Truco is first called (4 points)
            /// </summary>
            public const int TrucoCall = 4;            /// <summary>
            /// Maximum stakes value "Doze" (12 points)
            /// </summary>
            public const int Maximum = 12;

            /// <summary>
            /// Amount to raise stakes by (+4 each time: 2→4→8→12)
            /// </summary>
            public const int RaiseAmount = 4;
        }

        /// <summary>
        /// Game setup constants
        /// </summary>
        public static class Game
        {
            /// <summary>
            /// Maximum number of players in a Truco game
            /// </summary>
            public const int MaxPlayers = 4;

            /// <summary>
            /// Score needed to win the game
            /// </summary>
            public const int WinningScore = 12;

            /// <summary>
            /// Number of cards dealt to each player per hand
            /// </summary>
            public const int CardsPerPlayer = 3;            /// <summary>
            /// Maximum number of rounds per hand
            /// </summary>
            public const int MaxRoundsPerHand = 3;

            /// <summary>
            /// First round number
            /// </summary>
            public const int FirstRound = 1;

            /// <summary>
            /// Minimum rounds needed to win a hand (best of 3)
            /// </summary>
            public const int RoundsToWinHand = 2;

            /// <summary>
            /// Human player seat number
            /// </summary>
            public const int HumanPlayerSeat = 0;
        }

        /// <summary>
        /// AI and automation constants
        /// </summary>
        public static class AI
        {
            /// <summary>
            /// Maximum iterations to prevent infinite loops in AI processing
            /// </summary>
            public const int MaxIterations = 10;

            /// <summary>
            /// Default delay in milliseconds before starting a new hand
            /// </summary>
            public const int NewHandDelayMs = 1000;
        }

        /// <summary>
        /// Team configuration constants
        /// </summary>
        public static class Teams
        {
            /// <summary>
            /// Name for the player's team (seats 0 and 2)
            /// </summary>
            public const string PlayerTeam = "Player's Team";

            /// <summary>
            /// Name for the opponent team (seats 1 and 3)
            /// </summary>
            public const string OpponentTeam = "Opponent Team";
        }

        /// <summary>
        /// Card and fold constants
        /// </summary>
        public static class Cards
        {
            /// <summary>
            /// Special card value used to represent a fold (lowest possible value)
            /// </summary>
            public const string FoldValue = "FOLD";

            /// <summary>
            /// Special suit used to represent a fold
            /// </summary>
            public const string FoldSuit = "FOLD";

            /// <summary>
            /// Card strength value for a fold (guaranteed lowest)
            /// </summary>
            public const int FoldStrength = -1;
        }
    }
}
