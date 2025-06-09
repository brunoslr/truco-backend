using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service responsible for managing game state cleanup operations
    /// </summary>
    public class GameStateCleanupService : IGameCleanupService
    {
        /// <summary>
        /// Clean up after a round is completed
        /// </summary>
        /// <param name="game">The current game state</param>
        public void CleanupAfterRound(GameState game)
        {
            // Clear played cards for the completed round
            foreach (var playedCard in game.PlayedCards)
            {
                playedCard.Card = Card.CreateFoldCard();
            }
        }

        /// <summary>
        /// Clean up after a hand is completed
        /// </summary>
        /// <param name="game">The current game state</param>
        public Task CleanupAfterHandAsync(GameState game)
        {
            // Clear all hand-related state
            game.PlayedCards.Clear();
            game.RoundWinners.Clear();
            game.RoundHistory.Clear();
            game.CurrentRound = TrucoConstants.Game.FirstRound;
            
            // Reset stakes and truco state
            game.Stakes = TrucoConstants.Stakes.Initial;
            game.IsTrucoCalled = false;
            game.IsRaiseEnabled = true;
            
            // Clear all player hands
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
                player.IsActive = false;
            }

            return Task.CompletedTask;
        }/// <summary>
        /// Clean up when game is completed
        /// </summary>
        /// <param name="game">The current game state</param>
        public void CleanupAfterGame(GameState game)
        {
            game.Status = GameStatus.Completed;
            
            // Clear all active states
            foreach (var player in game.Players)
            {
                player.IsActive = false;
            }
        }

        /// <summary>
        /// Initialize played card slots for a new round
        /// </summary>
        /// <param name="game">The current game state</param>
        public void InitializePlayedCardsForRound(GameState game)
        {
            // Ensure we have played card slots for all players
            for (int seat = 0; seat < TrucoConstants.Game.MaxPlayers; seat++)
            {
                var existingSlot = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == seat);                if (existingSlot == null)
                {
                    game.PlayedCards.Add(new PlayedCard(seat));
                }
                else
                {
                    existingSlot.Card = Card.CreateFoldCard();
                }
            }
        }

        /// <summary>
        /// Initialize played card slots for a new hand
        /// </summary>
        /// <param name="game">The current game state</param>
        public void InitializePlayedCardsForHand(GameState game)
        {
            game.PlayedCards.Clear();
            for (int seat = 0; seat < TrucoConstants.Game.MaxPlayers; seat++)
            {
                game.PlayedCards.Add(new PlayedCard(seat));
            }
        }
    }
}
