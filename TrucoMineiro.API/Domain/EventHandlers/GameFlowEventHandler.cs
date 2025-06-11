using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for managing game flow after game events
    /// </summary>
    public class GameFlowEventHandler : 
        IEventHandler<CardPlayedEvent>,
        IEventHandler<TrucoRaiseEvent>,
        IEventHandler<FoldHandEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGameStateManager _gameStateManager;
        private readonly IHandResolutionService _handResolutionService;
        private readonly ILogger<GameFlowEventHandler> _logger;

        public GameFlowEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameStateManager gameStateManager,
            IHandResolutionService handResolutionService,
            ILogger<GameFlowEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _gameStateManager = gameStateManager;
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
                }                _logger.LogDebug("Processing game flow for card played in game {GameId} by player {PlayerName} (seat {PlayerSeat})", 
                    gameEvent.GameId, gameEvent.Player.Name, gameEvent.Player.Seat);                // Advance to next player
                _gameStateManager.AdvanceToNextPlayer(game);

                // Check if round is complete
                if (_gameStateManager.IsRoundComplete(game))
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
                            pc.Card = Card.CreateEmptyCard();
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
                {                    // Continue with next player
                    var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                    if (activePlayer != null)
                    {                        _logger.LogDebug("Next player turn in game {GameId}: player {PlayerSeat}", 
                            gameEvent.GameId, activePlayer.Seat);

                        var nextTurnEvent = new PlayerTurnStartedEvent(
                            gameEvent.GameId,
                            activePlayer,
                            game.CurrentRound,
                            game.CurrentHand,
                            game,
                            new List<string> { "play-card" }                        );
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

        /// <summary>
        /// Handle truco/raise events and manage game flow
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
                    gameEvent.GameId, gameEvent.Player.Name, gameEvent.Player.Seat);                // Update game stakes
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
                    var availableActions = new List<string> { "accept-truco", "fold", "raise-truco" };
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
        /// Handle fold events and manage game completion
        /// </summary>
        public async Task HandleAsync(FoldHandEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for fold processing", gameEvent.GameId);
                    return;
                }

                _logger.LogDebug("Processing fold in game {GameId} by player {PlayerName} (seat {PlayerSeat})", 
                    gameEvent.GameId, gameEvent.Player.Name, gameEvent.Player.Seat);

                // Award points to the winning team
                var winningTeam = gameEvent.WinningTeam;
                if (winningTeam == "Team 1")
                {
                    game.Team1Score += gameEvent.CurrentStakes;
                }
                else if (winningTeam == "Team 2")
                {
                    game.Team2Score += gameEvent.CurrentStakes;
                }                // Check if game is complete (reached winning score)
                if (game.Team1Score >= 12 || game.Team2Score >= 12)
                {
                    game.Status = GameStatus.Completed;
                    
                    _logger.LogInformation("Game {GameId} completed. Final scores - Team 1: {Team1Score}, Team 2: {Team2Score}", 
                        gameEvent.GameId, game.Team1Score, game.Team2Score);
                }
                else
                {                    // Start new hand
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
                }

                // Save updated game state
                await _gameRepository.SaveGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game flow for fold event in game {GameId}", gameEvent.GameId);
            }
        }
    }
}
