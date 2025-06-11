using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Specialized event handler for managing game flow after Truco calls and raises
    /// </summary>
    public class TrucoResponseFlowEventHandler : IEventHandler<TrucoRaiseEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<TrucoResponseFlowEventHandler> _logger;

        public TrucoResponseFlowEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            ILogger<TrucoResponseFlowEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        /// <summary>
        /// Handles TrucoRaiseEvent by managing stakes updates and response player activation
        /// </summary>
        public async Task HandleAsync(TrucoRaiseEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for truco/raise processing", gameEvent.GameId);
                    return;
                }

                _logger.LogDebug("Processing truco/raise in game {GameId} by player {PlayerName} (seat {PlayerSeat})", 
                    gameEvent.GameId, gameEvent.Player.Name, gameEvent.Player.Seat);

                // Update game stakes
                game.Stakes = gameEvent.NewStakes;

                // Set accepting player as active (the player who needs to respond)
                game.Players.ForEach(p => p.IsActive = false);
                
                // Find the opponent who needs to respond (next player in turn order)
                var opposingPlayers = game.Players.Where(p => p.Seat != gameEvent.Player.Seat).ToList();
                var nextResponder = opposingPlayers.FirstOrDefault(); // In Truco, typically the next player responds
                
                if (nextResponder != null)
                {
                    nextResponder.IsActive = true;
                    game.CurrentPlayerIndex = nextResponder.Seat;

                    // Publish turn event for the responding player with truco response options
                    var availableActions = new List<string> { "accept-truco", "surrender", "raise-truco" };
                    var nextTurnEvent = new PlayerTurnStartedEvent(
                        gameEvent.GameId,
                        nextResponder,
                        game.CurrentRound,
                        game.CurrentHand,
                        game,
                        availableActions
                    );
                    await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
                }

                // Save updated game state
                await _gameRepository.SaveGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game flow for truco/raise event in game {GameId}", gameEvent.GameId);
            }
        }

        /// <summary>
        /// Indicates whether this handler can process the given event
        /// </summary>
        public bool CanHandle(TrucoRaiseEvent gameEvent) => true;
    }
}
