using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests.TestUtilities
{
    /// <summary>
    /// Factory class for creating test game instances with consistent baseline state
    /// </summary>
    public static class TestGameFactory
    {
        /// <summary>
        /// Creates a test game with properly initialized players and state
        /// </summary>
        /// <returns>A GameState instance ready for testing</returns>
        public static GameState CreateTestGame()
        {
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            
            // Ensure AI players are properly marked
            game.Players[1].IsAI = true;
            game.Players[2].IsAI = true; // Partner
            game.Players[3].IsAI = true;

            return game;
        }        /// <summary>
        /// Creates a test game with all cards played (empty hands)
        /// Sets up PlayedCards collection to simulate a completed round
        /// </summary>
        /// <returns>A GameState instance with no cards in player hands and all players having played cards</returns>
        public static GameState CreateGameWithAllCardsPlayed()
        {
            var game = CreateTestGame();

            // Clear all player hands to simulate all cards played
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
            }

            SetupPlayedCardsFilled(game);

            return game;
        }        /// <summary>
        /// Creates a test game with some cards remaining in player hands
        /// </summary>
        /// <returns>A GameState instance with cards still in player hands</returns>
        public static GameState CreateGameWithSomeCardsRemaining()
        {
            var game = CreateTestGame();
            
            // Leave some cards in player hands (keep first card for each player)
            foreach (var player in game.Players)
            {
                if (player.Hand.Count > 1)
                {
                    player.Hand.RemoveRange(1, player.Hand.Count - 1);
                }
            }

            // Set up round winners for the first two rounds (one for each team)
            // This means the hand is not complete yet (need 2 wins for one team)
            game.RoundWinners.Add(1); // Team 1 won first round
            game.RoundWinners.Add(2); // Team 2 won second round
            // Current round will be the third round
            
            // Set up played cards to simulate that current round is complete
            SetupPlayedCardsFilled(game);

            return game;
        }

        /// <summary>
        /// Creates a test game ready for round completion testing
        /// </summary>
        /// <returns>A GameState instance with all players having played cards in current round</returns>
        public static GameState CreateGameReadyForRoundCompletion()
        {
            var game = CreateTestGame();
            
            // Set up played cards for all players to simulate round completion
            game.PlayedCards.Clear();
            game.PlayedCards.Add(new PlayedCard(0, new Card("7", "♥")));
            game.PlayedCards.Add(new PlayedCard(1, new Card("Q", "♦")));
            game.PlayedCards.Add(new PlayedCard(2, new Card("J", "♠")));
            game.PlayedCards.Add(new PlayedCard(3, new Card("K", "♣")));

            return game;
        }
        
        private static void SetupPlayedCardsFilled(GameState game)
        {
            // Set up played cards for all players to simulate round completion
            // This ensures IsRoundComplete() returns true
            game.PlayedCards.Clear();
            game.PlayedCards.Add(new PlayedCard(0, new Card("7", "♥")));
            game.PlayedCards.Add(new PlayedCard(1, new Card("Q", "♦")));
            game.PlayedCards.Add(new PlayedCard(2, new Card("J", "♠")));
            game.PlayedCards.Add(new PlayedCard(3, new Card("K", "♣")));
        }
    }
}
