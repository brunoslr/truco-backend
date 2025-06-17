using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.EventHandlers
{    /// <summary>
    /// Handles HandStartedEvent to apply last hand rules and reset truco state
    /// </summary>
    public class HandStartedEventHandler : IEventHandler<HandStartedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ITrucoRulesEngine _trucoRulesEngine;
        private readonly ILogger<HandStartedEventHandler> _logger;

        public HandStartedEventHandler(
            IGameRepository gameRepository,
            ITrucoRulesEngine trucoRulesEngine,
            ILogger<HandStartedEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _trucoRulesEngine = trucoRulesEngine;
            _logger = logger;
        }

        public async Task HandleAsync(HandStartedEvent eventArgs, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(eventArgs.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found when handling HandStartedEvent", eventArgs.GameId);
                    return;
                }                _logger.LogInformation("Processing HandStartedEvent for game {GameId}, hand {HandNumber}", 
                    eventArgs.GameId, eventArgs.Hand);

                // Reset truco state for new hand (in case it wasn't reset properly)
                ResetTrucoState(game);                // Apply last hand rules if applicable
                if (_trucoRulesEngine.IsLastHand(game))
                {
                    _logger.LogInformation("Applying last hand rules for game {GameId}", eventArgs.GameId);
                    _trucoRulesEngine.ApplyLastHandRule(game);
                }

                // Save the updated game state
                await _gameRepository.SaveGameAsync(game);

                _logger.LogInformation("HandStartedEvent processed successfully for game {GameId}", eventArgs.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing HandStartedEvent for game {GameId}", eventArgs.GameId);
                throw;
            }
        }        /// <summary>
        /// Reset truco-related state for the new hand
        /// </summary>
        private void ResetTrucoState(GameState game)
        {
            // Only reset if not applying last hand rules
            if (!_trucoRulesEngine.IsLastHand(game))            {
                game.Stakes = TrucoConstants.Stakes.Initial; // 2 points
                game.TrucoCallState = TrucoCallState.None;
                game.LastTrucoCallerTeam = -1;
                game.CanRaiseTeam = null;
            }
        }
    }
}
