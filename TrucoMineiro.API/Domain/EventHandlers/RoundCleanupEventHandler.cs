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
        private readonly IEventPublisher _eventPublisher;

        public RoundCleanupEventHandler(IGameRepository gameRepository, IEventPublisher eventPublisher)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
        }

        /// <summary>
        /// Handle round completed events and perform round cleanup
        /// </summary>
        public async Task HandleAsync(RoundCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = gameEvent.GameState;
            
            // Clear played cards for the completed round
            ClearRoundPlayedCards(game);
            
            // Reset card play slots for next round (if not hand complete)
            PreparePlayedCardsForNextRound(game);
            
            // Save the updated game state
            await _gameRepository.SaveGameAsync(game);
        }

        /// <summary>
        /// Clear the cards that were played in the completed round
        /// </summary>
        private static void ClearRoundPlayedCards(GameState game)
        {
            // Reset each played card slot
            foreach (var playedCard in game.PlayedCards)
            {
                playedCard.Card = null;
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
