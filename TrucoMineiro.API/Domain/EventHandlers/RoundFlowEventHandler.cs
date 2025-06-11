using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Specialized event handler for managing round progression after card plays
    /// </summary>
    public class RoundFlowEventHandler : IEventHandler<CardPlayedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGameStateManager _gameStateManager;
        private readonly IHandResolutionService _handResolutionService;
        private readonly ILogger<RoundFlowEventHandler> _logger;

        public RoundFlowEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameStateManager gameStateManager,
            IHandResolutionService handResolutionService,
            ILogger<RoundFlowEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _gameStateManager = gameStateManager;
            _handResolutionService = handResolutionService;
            _logger = logger;
        }        
        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for round flow processing", gameEvent.GameId);
                    return;
                }               

                // Advance to next player
                _gameStateManager.AdvanceToNextPlayer(game);
                // Check if round is complete
                if (_gameStateManager.IsRoundComplete(game))
                {
                    await HandleRoundCompletion(game, gameEvent.GameId, cancellationToken);
                }
                else
                {
                    await HandleNextPlayerTurn(game, gameEvent.GameId, cancellationToken);
                }

                // Save updated game state
                await _gameRepository.SaveGameAsync(game);
            }            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing round flow for card played event in game {GameId}", gameEvent.GameId);
            }
        }

        private async Task HandleRoundCompletion(GameState game, Guid gameId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Round complete in game {GameId}, determining winner", gameId);

            // Determine round winner using hand resolution service
            var playedCards = game.PlayedCards.Where(pc => pc.Card != null).ToList();
            var winner = _handResolutionService.DetermineRoundWinner(playedCards, game.Players);
            
            // Publish round completed event
            var roundPlayedCards = playedCards.Select(pc => pc.Card!).ToList();
            var roundCompletedEvent = new RoundCompletedEvent(
                gameId,
                game.CurrentRound,
                game.CurrentHand,
                winner,
                roundPlayedCards,
                new Dictionary<Guid, int>(), // Score changes - can be calculated if needed
                game,
                winner == null // isDraw when no winner
            );
            await _eventPublisher.PublishAsync(roundCompletedEvent, cancellationToken);            // Determine if hand is complete or new round should start
            if (AreAllCardsPlayed(game))
            {
                HandleHandCompletion(game, gameId);
            }
            else
            {
                await HandleNewRoundStart(game, gameId, winner, cancellationToken);
            }
        }        private async Task HandleNextPlayerTurn(GameState game, Guid gameId, CancellationToken cancellationToken)
        {
            var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
            if (activePlayer != null)
            {
                _logger.LogInformation("🔄 RoundFlowEventHandler: Publishing PlayerTurnStartedEvent for {PlayerName} (seat {PlayerSeat}, IsAI: {IsAI})", 
                    activePlayer.Name, activePlayer.Seat, activePlayer.IsAI);

                var nextTurnEvent = new PlayerTurnStartedEvent(
                    gameId,
                    activePlayer,
                    game.CurrentRound,
                    game.CurrentHand,
                    game,
                    new List<string> { "play-card" }
                );
                await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
                
                _logger.LogInformation("🔄 RoundFlowEventHandler: PlayerTurnStartedEvent published successfully");
            }
            else
            {
                _logger.LogWarning("🔄 RoundFlowEventHandler: No active player found for next turn in game {GameId}", gameId);
            }
        }

        private async Task HandleNewRoundStart(GameState game, Guid gameId, Player? winner, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting new round in game {GameId}, winner {WinnerName} plays first", 
                gameId, winner?.Name ?? "Draw");

            // Clear played cards for next round
            foreach (var pc in game.PlayedCards)
            {
                pc.Card = Card.CreateEmptyCard();
            }

            // Winner of round plays first in next round (or first player if draw)
            game.Players.ForEach(p => p.IsActive = false);
            var nextPlayer = winner ?? game.Players.First();
            nextPlayer.IsActive = true;
            game.CurrentPlayerIndex = nextPlayer.Seat;

            // Increment round counter
            game.CurrentRound++;

            // Publish next player turn event
            var nextTurnEvent = new PlayerTurnStartedEvent(
                gameId,
                nextPlayer,
                game.CurrentRound,
                game.CurrentHand,
                game,
                new List<string> { "play-card" }
            );
            await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
        }        private void HandleHandCompletion(GameState game, Guid gameId)
        {
            _logger.LogDebug("Hand complete in game {GameId}", gameId);
            
            // Hand completion will be handled by HandCompletionEventHandler
            // For now, just log it
            _logger.LogInformation("Hand completed in game {GameId} - Hand completion logic to be handled by specialized handler", gameId);
        }

        private static bool AreAllCardsPlayed(GameState game)
        {
            return game.Players.All(p => p.Hand.Count == 0);
        }
    }
}
