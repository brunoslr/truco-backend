using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for cleaning up game state after rounds are completed
    /// </summary>
    public class RoundCleanupEventHandler : IEventHandler<RoundCompletedEvent>
    {
        private readonly IGameRepository _gameRepository;

        public RoundCleanupEventHandler(IGameRepository gameRepository)
        {
            _gameRepository = gameRepository;
        }        
          /// <summary>
        /// Handle round completed events and perform round cleanup
        /// </summary>
        public async Task HandleAsync(RoundCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = gameEvent.GameState;
            
            // Save the completed round to history before clearing
            SaveRoundToHistory(game);
            
            // Update RoundWinners list with the winning team number
            UpdateRoundWinners(game, gameEvent.RoundWinner);
            
            // Clear played cards for the completed round
            ClearRoundPlayedCards(game);
            
            // Advance to next round if hand not complete
            if (game.CurrentRound < TrucoConstants.Game.MaxRoundsPerHand)
            {
                game.CurrentRound++;
                
                // Reset card play slots for next round
                PreparePlayedCardsForNextRound(game);
            }
            
            // Save the updated game state
            await _gameRepository.SaveGameAsync(game);
        }        /// <summary>
        /// Save the current round's played cards to the round history
        /// </summary>
        private static void SaveRoundToHistory(GameState game)
        {
            // Save current round's played cards to history
            var currentRoundCards = game.PlayedCards.Where(pc => pc.Card != null).ToList();
            if (currentRoundCards.Any())
            {
                game.RoundHistory[game.CurrentRound] = currentRoundCards;
            }
        }

        /// <summary>
        /// Update the RoundWinners list with the winning team number
        /// </summary>
        private static void UpdateRoundWinners(GameState game, Player? roundWinner)
        {
            if (roundWinner != null)
            {
                // Determine team number from player seat
                // Team 1: seats 0, 2; Team 2: seats 1, 3
                var teamNumber = (roundWinner.Seat % 2 == 0) ? 1 : 2;
                game.RoundWinners.Add(teamNumber);
            }
            else
            {
                // In case of a draw, we could add 0 or just skip
                // For now, let's not add anything for draws
            }
        }
        
        /// <summary>
        /// Clear the cards that were played in the completed round
        /// </summary>
        private static void ClearRoundPlayedCards(GameState game)
        {
            // Reset each played card slot
            foreach (var playedCard in game.PlayedCards)
            {
                playedCard.Card = Card.CreateEmptyCard();
            }
        }

        /// <summary>
        /// Ensure PlayedCards slots exist for all players for the next round
        /// </summary>
        private static void PreparePlayedCardsForNextRound(GameState game)
        {
            // Ensure we have played card slots for all players
            for (int seat = 0; seat < game.Players.Count; seat++)
            {
                var existingSlot = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == seat);
                if (existingSlot == null)
                {
                    game.PlayedCards.Add(new PlayedCard(seat));
                }
            }
        }
    }
}
