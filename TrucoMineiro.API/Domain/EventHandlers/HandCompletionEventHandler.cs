using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Handles hand completion events, including hand surrender scenarios.
    /// Responsible for awarding stakes, checking game completion, and starting new hands.
    /// </summary>
    public class HandCompletionEventHandler : IEventHandler<SurrenderHandEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGameStateManager _gameStateManager;
        private readonly ILogger<HandCompletionEventHandler> _logger;

        public HandCompletionEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameStateManager gameStateManager,
            ILogger<HandCompletionEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _gameStateManager = gameStateManager;
            _logger = logger;
        }

        public async Task HandleAsync(SurrenderHandEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for hand surrender processing", gameEvent.GameId);
                    return;
                }

                _logger.LogDebug("Processing hand surrender in game {GameId} by player {PlayerName} (seat {PlayerSeat})", 
                    gameEvent.GameId, gameEvent.Player.Name, gameEvent.Player.Seat);

                // Award stakes to the winning team
                await AwardStakesToWinningTeam(game, gameEvent);

                // Check if game is complete or continue with new hand
                if (IsGameComplete(game))
                {
                    await CompleteGame(game, gameEvent);
                }
                else
                {
                    await StartNewHand(game, gameEvent, cancellationToken);
                }

                // Save updated game state
                await _gameRepository.SaveGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing hand surrender event in game {GameId}", gameEvent.GameId);
            }
        }

        private async Task AwardStakesToWinningTeam(GameState game, SurrenderHandEvent gameEvent)
        {
            var winningTeam = gameEvent.WinningTeam;
            if (winningTeam == "Team 1")
            {
                game.Team1Score += gameEvent.CurrentStake;
                _logger.LogDebug("Team 1 awarded {Stakes} points in game {GameId}", gameEvent.CurrentStake, gameEvent.GameId);
            }
            else if (winningTeam == "Team 2")
            {
                game.Team2Score += gameEvent.CurrentStake;
                _logger.LogDebug("Team 2 awarded {Stakes} points in game {GameId}", gameEvent.CurrentStake, gameEvent.GameId);
            }
        }

        private bool IsGameComplete(GameState game)
        {
            return game.Team1Score >= 12 || game.Team2Score >= 12;
        }

        private async Task CompleteGame(GameState game, SurrenderHandEvent gameEvent)
        {
            game.GameStatus = "completed";
            
            _logger.LogInformation("Game {GameId} completed. Final scores - Team 1: {Team1Score}, Team 2: {Team2Score}", 
                gameEvent.GameId, game.Team1Score, game.Team2Score);
        }

        private async Task StartNewHand(GameState game, SurrenderHandEvent gameEvent, CancellationToken cancellationToken)
        {
            // Start new hand
            _gameStateManager.StartNewHand(game);
            
            // Clear played cards
            foreach (var pc in game.PlayedCards)
            {
                pc.Card = Card.CreateEmptyCard();
            }

            // Set first player active for new hand
            game.Players.ForEach(p => p.IsActive = false);
            var firstPlayer = game.Players.First();
            firstPlayer.IsActive = true;
            game.CurrentPlayerIndex = firstPlayer.Seat;

            // Publish new hand start event
            var nextTurnEvent = new PlayerTurnStartedEvent(
                gameEvent.GameId,
                firstPlayer,
                1, // New hand starts at round 1
                game.CurrentHand,
                game,
                new List<string> { "play-card", "truco" }
            );
            await _eventPublisher.PublishAsync(nextTurnEvent, cancellationToken);

            _logger.LogDebug("Started new hand {HandNumber} in game {GameId}, first player: {PlayerName} (seat {PlayerSeat})", 
                game.CurrentHand, gameEvent.GameId, firstPlayer.Name, firstPlayer.Seat);
        }
    }
}
