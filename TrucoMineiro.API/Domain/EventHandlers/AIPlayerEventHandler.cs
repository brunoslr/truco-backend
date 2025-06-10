using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for AI player decision making in response to game events
    /// </summary>
    public class AIPlayerEventHandler : IEventHandler<PlayerTurnStartedEvent>
    {
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<AIPlayerEventHandler> _logger;

        public AIPlayerEventHandler(
            IAIPlayerService aiPlayerService,
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            ILogger<AIPlayerEventHandler> logger)
        {
            _aiPlayerService = aiPlayerService;
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }        /// <summary>
        /// Handle AI player turn events
        /// </summary>
        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for AI turn", gameEvent.GameId);
                    return;
                }

                var player = gameEvent.Player;
                if (player == null || !player.IsAI)
                {
                    // Not an AI player, ignore this event
                    _logger.LogDebug("Player {PlayerName} at seat {PlayerSeat} in game {GameId} is not an AI player", 
                        player?.Name, player?.Seat, gameEvent.GameId);
                    return;
                }

                _logger.LogDebug("AI player {PlayerName} (seat {PlayerSeat}) thinking in game {GameId}", 
                    player.Name, player.Seat, gameEvent.GameId);                // Add thinking delay for realism
                await Task.Delay(GetAIThinkingDelay(), cancellationToken);

                // AI makes decision
                var cardIndex = _aiPlayerService.SelectCardToPlay(player, game);
                
                if (cardIndex >= 0 && cardIndex < player.Hand.Count)
                {
                    var card = player.Hand[cardIndex];
                    player.Hand.RemoveAt(cardIndex);

                    // Update game state - add card to played cards
                    var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
                    if (playedCard != null)
                    {
                        playedCard.Card = card;
                    }
                    else                    {
                        game.PlayedCards.Add(new PlayedCard(player.Seat, card));
                    }

                    // ActionLog entry will be created by ActionLogEventHandler when CardPlayedEvent is published

                    // Save game state
                    await _gameRepository.SaveGameAsync(game);                    // Publish card played event to trigger game flow
                    var cardPlayedEvent = new CardPlayedEvent(
                        gameEvent.GameId,
                        player.Id,  // Already a Guid
                        card,
                        player,
                        gameEvent.Round,
                        gameEvent.Hand,
                        true, // isAIMove
                        game
                    );

                    await _eventPublisher.PublishAsync(cardPlayedEvent, cancellationToken);

                    _logger.LogDebug("AI player {PlayerName} (seat {PlayerSeat}) played {Card} in game {GameId}", 
                        player.Name, player.Seat, $"{card.Value} of {card.Suit}", gameEvent.GameId);
                }
                else
                {
                    _logger.LogWarning("AI player {PlayerName} (seat {PlayerSeat}) in game {GameId} could not select a valid card", 
                        player.Name, player.Seat, gameEvent.GameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI turn for player {PlayerName} (seat {PlayerSeat}) in game {GameId}", 
                    gameEvent.Player?.Name, gameEvent.Player?.Seat, gameEvent.GameId);
            }
        }        /// <summary>
        /// Generate realistic AI thinking delay
        /// </summary>
        private TimeSpan GetAIThinkingDelay()
        {
            // Random delay between min and max AI play delay for realism
            var random = new Random();
            return TimeSpan.FromMilliseconds(random.Next(GameConfiguration.DefaultMinAIPlayDelayMs, GameConfiguration.DefaultMaxAIPlayDelayMs));
        }
    }
}
