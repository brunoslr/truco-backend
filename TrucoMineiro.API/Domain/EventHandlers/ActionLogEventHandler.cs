using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for creating ActionLogEntry records from game events for frontend display
    /// </summary>
    public class ActionLogEventHandler : 
        IEventHandler<CardPlayedEvent>,
        IEventHandler<PlayerTurnStartedEvent>,
        IEventHandler<RoundCompletedEvent>,
        IEventHandler<TrucoOrRaiseCalledEvent>,
        IEventHandler<TrucoRaiseEvent>,
        IEventHandler<SurrenderTrucoEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ILogger<ActionLogEventHandler> _logger;

        public ActionLogEventHandler(
            IGameRepository gameRepository,
            ILogger<ActionLogEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _logger = logger;
        }

        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for action log creation", gameEvent.GameId);
                    return;
                }                // Create action log entry for card played
                var cardDisplay = gameEvent.Card.IsFold 
                    ? "FOLD" 
                    : $"{gameEvent.Card.Value} of {gameEvent.Card.Suit}";
                  var actionLogEntry = new ActionLogEntry("card-played")
                {
                    PlayerSeat = gameEvent.Player.Seat,
                    Card = cardDisplay,
                    RoundNumber = gameEvent.Round // Use round number from event
                };

                game.ActionLog.Add(actionLogEntry);
                await _gameRepository.SaveGameAsync(game);

                _logger.LogDebug("Added action log entry for card played: Player {PlayerSeat} played {Card} in game {GameId}", 
                    gameEvent.Player.Seat, cardDisplay, gameEvent.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry for card played event in game {GameId}", gameEvent.GameId);
            }
        }

        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for action log creation", gameEvent.GameId);
                    return;
                }                // Only log turn starts for human players or significant game state changes
                if (!gameEvent.Player.IsAI)
                {                    var actionLogEntry = new ActionLogEntry("turn-start")
                    {
                        PlayerSeat = gameEvent.Player.Seat,
                        RoundNumber = gameEvent.Round // Use round number from event
                    };

                    game.ActionLog.Add(actionLogEntry);
                    await _gameRepository.SaveGameAsync(game);

                    _logger.LogDebug("Added action log entry for turn start: Player {PlayerSeat} in game {GameId}", 
                        gameEvent.Player.Seat, gameEvent.GameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry for turn started event in game {GameId}", gameEvent.GameId);
            }
        }

        public async Task HandleAsync(RoundCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for action log creation", gameEvent.GameId);
                    return;
                }                // Create action log entry for round result
                var actionLogEntry = new ActionLogEntry("turn-result")
                {
                    Winner = gameEvent.RoundWinner?.Team,                    
                    RoundNumber = gameEvent.Round // Use round number from event
                };

                game.ActionLog.Add(actionLogEntry);
                await _gameRepository.SaveGameAsync(game);                _logger.LogDebug("Added action log entry for round completed: Winner {Winner} in game {GameId}",
                    gameEvent.RoundWinner?.ToString() ?? "None", gameEvent.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry for round completed event in game {GameId}", gameEvent.GameId);
            }
        }

        public async Task HandleAsync(TrucoRaiseEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for action log creation", gameEvent.GameId);
                    return;
                }                // Create action log entry for truco/raise
                var action = gameEvent.IsInitialTruco ? "called Truco" : "raised stakes";                var actionLogEntry = new ActionLogEntry("button-pressed")
                {
                    PlayerSeat = gameEvent.Player.Seat,
                    Action = $"{action} to {gameEvent.NewStakes} points",
                    RoundNumber = game.CurrentRound // Use current round from game state
                };

                game.ActionLog.Add(actionLogEntry);
                await _gameRepository.SaveGameAsync(game);

                _logger.LogDebug("Added action log entry for truco/raise: Player {PlayerSeat} {Action} in game {GameId}", 
                    gameEvent.Player.Seat, action, gameEvent.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry for truco/raise event in game {GameId}", gameEvent.GameId);
            }
        }        public async Task HandleAsync(SurrenderTrucoEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for action log creation", gameEvent.GameId);
                    return;
                }

                // Create action log entry for hand surrender
                var actionLogEntry = new ActionLogEntry("button-pressed")
                {
                    PlayerSeat = gameEvent.Player.Seat,
                    Action = $"surrendered hand, {gameEvent.WinningTeam} gains {gameEvent.CurrentStake} points",
                    RoundNumber = game.CurrentRound // Use current round from game state
                };

                game.ActionLog.Add(actionLogEntry);

                // Also add hand result entry
                var handResultEntry = new ActionLogEntry("hand-result")
                {
                    HandNumber = gameEvent.HandNumber,
                    Winner = gameEvent.WinningTeam
                };

                game.ActionLog.Add(handResultEntry);
                await _gameRepository.SaveGameAsync(game);

                _logger.LogDebug("Added action log entries for fold: Player {PlayerSeat} folded in game {GameId}", 
                    gameEvent.Player.Seat, gameEvent.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry for fold event in game {GameId}", gameEvent.GameId);
            }
        }

        public async Task HandleAsync(TrucoOrRaiseCalledEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for action log creation", gameEvent.GameId);
                    return;
                }                // Create action log entry for truco call
                var actionLogEntry = new ActionLogEntry("truco-called")
                {
                    PlayerSeat = gameEvent.CallingPlayer.Seat,
                    Action = $"{gameEvent.CallType} for {gameEvent.NewPotentialStakes} points"
                };

                game.ActionLog.Add(actionLogEntry);
                await _gameRepository.SaveGameAsync(game);

                _logger.LogDebug("Added action log entry for truco call: Player {PlayerSeat} called {CallType} in game {GameId}", 
                    gameEvent.CallingPlayer.Seat, gameEvent.CallType, gameEvent.GameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating action log entry for truco call event in game {GameId}", gameEvent.GameId);
            }
        }
    }
}
