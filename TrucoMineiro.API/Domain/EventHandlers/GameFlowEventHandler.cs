using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for managing game flow after card played events
    /// </summary>
    public class GameFlowEventHandler : IEventHandler<CardPlayedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGameFlowService _gameFlowService;
        private readonly IHandResolutionService _handResolutionService;
        private readonly ILogger<GameFlowEventHandler> _logger;

        public GameFlowEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameFlowService gameFlowService,
            IHandResolutionService handResolutionService,
            ILogger<GameFlowEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _gameFlowService = gameFlowService;
            _handResolutionService = handResolutionService;
            _logger = logger;
        }

        /// <summary>
        /// Handle card played events and manage game flow
        /// </summary>
        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for game flow processing", gameEvent.GameId);
                    return;
                }

                _logger.LogDebug("Processing game flow for card played in game {GameId} by player {PlayerName} (seat {PlayerSeat})", 
                    gameEvent.GameId, gameEvent.Player.Name, gameEvent.Player.Seat);

                // Advance to next player
                _gameFlowService.AdvanceToNextPlayer(game);

                // Check if round is complete
                if (_gameFlowService.IsRoundComplete(game))
                {
                    _logger.LogDebug("Round complete in game {GameId}, determining winner", gameEvent.GameId);                    // Determine round winner using proper service
                    var playedCards = game.PlayedCards.Where(pc => pc.Card != null).ToList();
                    var winner = _handResolutionService.DetermineRoundWinner(playedCards, game.Players);
                    
                    // Publish round completed event
                    var roundPlayedCards = playedCards.Select(pc => pc.Card!).ToList();                    var roundCompletedEvent = new RoundCompletedEvent(
                        gameEvent.GameId,
                        game.CurrentRound,
                        game.CurrentHand,
                        winner,
                        roundPlayedCards,
                        new Dictionary<Guid, int>(), // Score changes - can be calculated if needed
                        game,
                        winner == null // isDraw when no winner
                    );
                    await _eventPublisher.PublishAsync(roundCompletedEvent, cancellationToken);

                    // Determine if hand is complete or new round should start
                    if (game.Players.All(p => p.Hand.Count == 0))
                    {
                        _logger.LogDebug("Hand complete in game {GameId}", gameEvent.GameId);
                        
                        // Hand is complete - this will be handled by future hand completion handler
                        // For now, just log it
                        _logger.LogInformation("Hand completed in game {GameId} - Hand completion logic needed", gameEvent.GameId);
                    }
                    else
                    {
                        _logger.LogDebug("Starting new round in game {GameId}, winner {WinnerName} plays first", 
                            gameEvent.GameId, winner?.Name ?? "Draw");                        // Clear played cards for next round
                        foreach (var pc in game.PlayedCards)
                        {
                            pc.Card = Card.CreateFoldCard();
                        }

                        // Winner of round plays first in next round (or first player if draw)
                        game.Players.ForEach(p => p.IsActive = false);
                        var nextPlayer = winner ?? game.Players.First();
                        nextPlayer.IsActive = true;
                        game.CurrentPlayerIndex = nextPlayer.Seat;

                        // Publish next player turn event
                        var nextTurnEvent = new PlayerTurnStartedEvent(
                            gameEvent.GameId,
                            nextPlayer,
                            game.CurrentRound,
                            game.CurrentHand,
                            game,
                            new List<string> { "play-card" }
                        );
                        await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
                    }
                }
                else
                {
                    // Continue with next player
                    var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                    if (activePlayer != null)
                    {
                        _logger.LogDebug("Next player turn in game {GameId}: player {PlayerSeat}", 
                            gameEvent.GameId, activePlayer.Seat);

                        var nextTurnEvent = new PlayerTurnStartedEvent(
                            gameEvent.GameId,
                            activePlayer,
                            game.CurrentRound,
                            game.CurrentHand,
                            game,
                            new List<string> { "play-card" }
                        );
                        await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
                    }
                }

                // Save updated game state
                await _gameRepository.SaveGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game flow for card played event in game {GameId}", gameEvent.GameId);
            }
        }
    }
}
