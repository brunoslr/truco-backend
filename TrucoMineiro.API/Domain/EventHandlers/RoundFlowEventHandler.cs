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
    /// Specialized event handler for managing round progression after card plays
    /// </summary>
    public class RoundFlowEventHandler : IEventHandler<CardPlayedEvent>    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGameStateManager _gameStateManager;
        private readonly IHandResolutionService _handResolutionService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RoundFlowEventHandler> _logger;

        public RoundFlowEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameStateManager gameStateManager,
            IHandResolutionService handResolutionService,
            IConfiguration configuration,
            ILogger<RoundFlowEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _gameStateManager = gameStateManager;
            _handResolutionService = handResolutionService;
            _configuration = configuration;
            _logger = logger;
        }        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
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
            );            await _eventPublisher.PublishAsync(roundCompletedEvent, cancellationToken);

            // Determine if hand is complete or new round should start
            if (AreAllCardsPlayed(game))
            {
                await HandleHandCompletion(game, gameId, cancellationToken);
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
                var nextTurnEvent = new PlayerTurnStartedEvent(
                    gameId,
                    activePlayer,
                    game.CurrentRound,
                    game.CurrentHand,
                    game,
                    new List<string> { "play-card" }
                );
                await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
            }
            else
            {
                _logger.LogWarning("No active player found for next turn in game {GameId}", gameId);
            }
        }        private async Task HandleNewRoundStart(GameState game, Guid gameId, Player? winner, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Starting new round in game {GameId}, winner {WinnerName} plays first",
                gameId, winner?.Name ?? "Draw");            // Add round resolution delay before starting next round
            var roundResolutionDelay = GetRoundResolutionDelay();

            _logger.LogDebug("Adding round resolution delay of {DelayMs}ms before starting next round in game {GameId}",
                roundResolutionDelay.TotalMilliseconds, gameId);

            if (roundResolutionDelay > TimeSpan.Zero)
            {
                await Task.Delay(roundResolutionDelay, cancellationToken);
            }

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
                new List<string> { "play-card" });
            await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);
        }

        private async Task HandleHandCompletion(GameState game, Guid gameId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Hand complete in game {GameId}", gameId);

            // Determine the winning team using hand resolution service
            var winningTeam = _handResolutionService.GetHandWinner(game);

            if (winningTeam.HasValue) // Ensure winningTeam is not null
            {
                // Publish hand completed event to trigger cleanup and new hand start
                var handCompletedEvent = new HandCompletedEvent(
                    gameId,
                    game.CurrentHand,
                    winningTeam.Value, 
                    game.RoundWinners.ToList(), // Copy the round winners list
                    game.Stakes, // Points to be awarded
                    game
                );

                await _eventPublisher.PublishAsync(handCompletedEvent, cancellationToken);

                _logger.LogInformation("Hand {HandNumber} completed in game {GameId}, winning team: {WinningTeam}",
                    game.CurrentHand, gameId, winningTeam.Value);
            }
            else
            {
                _logger.LogError("Hand {HandNumber} completed in game {GameId} but no winning team could be determined",
                    game.CurrentHand, gameId);
                throw new InvalidOperationException($"Unable to complete hand {game.CurrentHand} - winning team could not be determined");
            }
        }

        private static bool AreAllCardsPlayed(GameState game)
        {
            return game.Players.All(p => p.Hand.Count == 0);
        }

        /// <summary>
        /// Get round resolution delay using configuration values with fallback to defaults
        /// </summary>
        private TimeSpan GetRoundResolutionDelay()
        {
            var delayMs = _configuration.GetValue<int>("GameSettings:RoundResolutionDelayMs", GameConfiguration.DefaultRoundResolutionDelayMs);
            
            // If delay is 0 or negative, return immediately (for tests)
            if (delayMs <= 0)
            {
                return TimeSpan.Zero;
            }
            
            _logger.LogDebug("Round resolution delay configured: {DelayMs}ms", delayMs);
                
            return TimeSpan.FromMilliseconds(delayMs);
        }
    }
}
