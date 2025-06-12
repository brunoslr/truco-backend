using Microsoft.Extensions.Configuration;
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
        private readonly IConfiguration _configuration;

        public AIPlayerEventHandler(
            IAIPlayerService aiPlayerService,
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            ILogger<AIPlayerEventHandler> logger,
            IConfiguration configuration)
        {
            _aiPlayerService = aiPlayerService;
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;
            _configuration = configuration;
        }        
          /// <summary>
        /// Handle AI player turn events
        /// </summary>
        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {              try
            {
                // Check if AI auto-play is enabled
                var autoAiPlayEnabled = _configuration.GetValue<bool>("FeatureFlags:AutoAiPlay", true);                if (!autoAiPlayEnabled)
                {
                    return;
                }
                
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
                    return;                
                }                
                // Add thinking delay for realism
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
                    );                    await _eventPublisher.PublishAsync(cardPlayedEvent, cancellationToken);
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
        /// Generate realistic AI thinking delay using configuration values
        /// </summary>
        private TimeSpan GetAIThinkingDelay()
        {
            // Get the configured AI play delays with fallback to defaults
            var minDelayMs = _configuration.GetValue<int>("GameSettings:AIMinPlayDelayMs", GameConfiguration.DefaultMinAIPlayDelayMs);
            var maxDelayMs = _configuration.GetValue<int>("GameSettings:AIMaxPlayDelayMs", GameConfiguration.DefaultMaxAIPlayDelayMs);
            
            // If either delay is 0 or negative, return immediately (for tests)
            if (minDelayMs <= 0 || maxDelayMs <= 0)
            {
                return TimeSpan.Zero;
            }
            
            // Validate configuration: ensure min <= max, swap if needed
            if (minDelayMs > maxDelayMs)
            {
                _logger.LogWarning("AI configuration invalid: MinPlayDelayMs ({MinDelay}) > MaxPlayDelayMs ({MaxDelay}). Swapping values.",
                    minDelayMs, maxDelayMs);
                (minDelayMs, maxDelayMs) = (maxDelayMs, minDelayMs);
            }
            
            // Generate random delay between min and max values
            var random = new Random();
            var delayMs = random.Next(minDelayMs, maxDelayMs + 1); // +1 because Random.Next upper bound is exclusive
            
            _logger.LogDebug("AI thinking delay generated: {DelayMs}ms (range: {MinDelayMs}-{MaxDelayMs}ms)", 
                delayMs, minDelayMs, maxDelayMs);
                
            return TimeSpan.FromMilliseconds(delayMs);
        }
    }
}
