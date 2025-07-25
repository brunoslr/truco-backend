namespace TrucoMineiro.API.Constants
{
    /// <summary>
    /// Global constants for Truco Mineiro game rules and values
    /// </summary>
    public static class TrucoConstants
    {        /// <summary>
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
            public const int TrucoCall = 4;

            /// <summary>
            /// Stakes value for Seis (8 points)
            /// </summary>
            public const int Seis = 8;

            /// <summary>
            /// Stakes value for Nove (10 points)
            /// </summary>
            public const int Nove = 10;

            /// <summary>
            /// Maximum stakes value "Doze" (12 points)
            /// </summary>
            public const int Maximum = 12;

            /// <summary>
            /// Stakes progression array - represents the complete stakes sequence
            /// [Initial, Truco, Seis, Nove, Doze]
            /// </summary>
            public static readonly int[] Progression = { Initial, TrucoCall, Seis, Nove, Maximum };

            /// <summary>
            /// Gets the index of a stakes value in the progression array
            /// </summary>
            public static int GetProgressionIndex(int stakesValue)
            {
                for (int i = 0; i < Progression.Length; i++)
                {
                    if (Progression[i] == stakesValue)
                        return i;
                }
                return -1; // Not found
            }

            /// <summary>
            /// Gets the next stakes value in the progression, or -1 if at maximum
            /// </summary>
            public static int GetNextStakesValue(int currentStakes)
            {
                int currentIndex = GetProgressionIndex(currentStakes);
                if (currentIndex >= 0 && currentIndex < Progression.Length - 1)
                    return Progression[currentIndex + 1];
                return -1; // Already at maximum or invalid value
            }

            /// <summary>
            /// Gets the previous stakes value in the progression, or -1 if at minimum
            /// </summary>
            public static int GetPreviousStakesValue(int currentStakes)
            {
                int currentIndex = GetProgressionIndex(currentStakes);
                if (currentIndex > 0)
                    return Progression[currentIndex - 1];
                return -1; // Already at minimum or invalid value
            }
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


            /// <summary>
            /// Special card value used to represent a fold (lowest possible value)
            /// </summary>
            public const string EmptyValue = "EMPTY";

            /// <summary>
            /// Special suit used to represent a fold
            /// </summary>
            public const string EmptySuit = "EMPTY";

            /// <summary>
            /// Card strength value for a fold (guaranteed lowest)
            /// </summary>
            public const int EmptyStrength = 0;
        }

        /// <summary>
        /// Command type constants for state machine operations
        /// </summary>
        public static class Commands
        {
            public const string StartGame = "StartGame";
            public const string PlayCard = "PlayCard";
            public const string CallTrucoOrRaise = "CallTrucoOrRaise";
            public const string AcceptTruco = "AcceptTruco";
            public const string SurrenderTruco = "SurrenderTruco";
            public const string SurrenderHand = "SurrenderHand";
        }

        /// <summary>
        /// Button action constants for API requests
        /// </summary>
        public static class ButtonActions
        {
            public const string CallTrucoOrRaise = "CallTrucoOrRaise";
            public const string AcceptTruco = "AcceptTruco";
            public const string SurrenderTruco = "SurrenderTruco";
            public const string SurrenderHand = "SurrenderHand";
            public const string PlayCard = "PlayCard";
        }

        /// <summary>
        /// Player action constants for game state available actions
        /// </summary>
        public static class PlayerActions
        {
            public const string PlayCard = "play-card";
            public const string CallTrucoOrRaise = "call-truco-or-raise";
            public const string AcceptTruco = "accept-truco";
            public const string SurrenderTruco = "surrender-truco";
            public const string Fold = "fold";
        }
    }
}
