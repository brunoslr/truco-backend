using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Unified event handler for all types of hand completion scenarios.
    /// Handles both regular hand completion (2 of 3 rounds won) and surrender scenarios.
    /// Single responsibility: Process hand completion, award points, and manage game progression.
    /// </summary>
    public class HandCompletionEventHandler : 
        IEventHandler<HandCompletedEvent>, 
        IEventHandler<SurrenderTrucoEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HandCompletionEventHandler> _logger;

        public HandCompletionEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameStateManager gameStateManager,
            IConfiguration configuration,
            ILogger<HandCompletionEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Handle regular hand completion (2 of 3 rounds won)
        /// </summary>
        public async Task HandleAsync(HandCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = gameEvent.GameState;
            
            _logger.LogDebug("Processing regular hand completion in game {GameId}, winning team: {WinningTeam}", 
                gameEvent.GameId, gameEvent.WinningTeam);

            // Award points to winning team
            AwardPointsToTeam(game, gameEvent.WinningTeam, gameEvent.PointsAwarded);
            
            // Process hand completion
            await ProcessHandCompletion(game, gameEvent.GameId, cancellationToken);
        }

        /// <summary>
        /// Handle surrender scenarios
        /// </summary>
        public async Task HandleAsync(SurrenderTrucoEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
            if (game == null)
            {
                _logger.LogWarning("Game {GameId} not found for hand surrender processing", gameEvent.GameId);
                return;
            }

            _logger.LogDebug("Processing hand surrender in game {GameId} by player {PlayerName}, winning team: {WinningTeam}", 
                gameEvent.GameId, gameEvent.Player.Name, gameEvent.WinningTeam);

            // Award points to winning team
            AwardPointsToTeam(game, gameEvent.WinningTeam, gameEvent.CurrentStake);
            
            // Process hand completion
            await ProcessHandCompletion(game, gameEvent.GameId, cancellationToken);
        }

        /// <summary>
        /// Core hand completion processing logic shared by both scenarios
        /// </summary>
        private async Task ProcessHandCompletion(GameState game, Guid gameId, CancellationToken cancellationToken)
        {
            // Check if game is complete
            if (IsGameComplete(game))
            {
                await CompleteGame(game, gameId, cancellationToken);
            }
            else
            {
                await PrepareNextHand(game, gameId, cancellationToken);
            }

            // Save updated game state
            await _gameRepository.SaveGameAsync(game);
        }        
        
        /// <summary>
        /// Award points to the specified team
        /// </summary>
        private void AwardPointsToTeam(GameState game, Team winningTeam, int points)
        {
            if ((int)winningTeam == 1)
            {
                game.Team1Score += points;
                game.TeamScores[Team.PlayerTeam] = game.Team1Score;
                _logger.LogDebug("Team 1 awarded {Points} points", points);
            }
            else if ((int)winningTeam == 2)
            {
                game.Team2Score += points;
                game.TeamScores[Team.OpponentTeam] = game.Team2Score;
                _logger.LogDebug("Team 2 awarded {Points} points", points);
            }
            else
            {
                _logger.LogWarning("Unknown team identifier '{WinningTeam}' - could not award {Points} points", winningTeam, points);
            }
        }      

        /// <summary>
        /// Check if the game is complete (one team reached winning score)
        /// </summary>
        private static bool IsGameComplete(GameState game)
        {
            return game.Team1Score >= TrucoConstants.Game.WinningScore || 
                   game.Team2Score >= TrucoConstants.Game.WinningScore;
        }

        /// <summary>
        /// Complete the game and publish game completion event
        /// </summary>
        private async Task CompleteGame(GameState game, Guid gameId, CancellationToken cancellationToken)
        {
            game.GameStatus = "completed";
            
            // Calculate game duration
            var gameDuration = DateTime.UtcNow - game.CreatedAt;
            
            // Get winning team
            var winningTeam = GetWinningTeam(game);
            
            // Convert team scores to player scores dictionary
            var finalScores = new Dictionary<Guid, int>();
            foreach (var player in game.Players)
            {
                var teamScore = game.TeamScores.ContainsKey(player.Team) ? game.TeamScores[player.Team] : 0;
                finalScores[player.Id] = teamScore;
            }
            
            // Publish game completed event
            var gameCompletedEvent = new GameCompletedEvent(
                gameId,
                winningTeam,
                finalScores,
                game,
                gameDuration
            );
            await _eventPublisher.PublishAsync(gameCompletedEvent, cancellationToken);
            
            _logger.LogInformation("Game {GameId} completed. Final scores - Team 1: {Team1Score}, Team 2: {Team2Score}", 
                gameId, game.Team1Score, game.Team2Score);
        }

        /// <summary>
        /// Prepare for the next hand
        /// </summary>
        private async Task PrepareNextHand(GameState game, Guid gameId, CancellationToken cancellationToken)
        {
            // Clear all hand-related state
            ClearHandState(game);
            
            // Move to next hand
            game.CurrentHand++;
            
            // Add hand resolution delay before starting next hand
            var handResolutionDelay = GetHandResolutionDelay();
            if (handResolutionDelay > TimeSpan.Zero)
            {
                await Task.Delay(handResolutionDelay, cancellationToken);
            }
            
            // Rotate dealer and set first player
            RotateDealer(game);
            
            // Deal new cards
            DealNewCards(game);
            
            // Initialize played cards slots for new hand
            InitializePlayedCardsSlots(game);
            
            // Publish hand started event
            var handStartedEvent = new HandStartedEvent(
                gameId,
                game.CurrentHand,
                game.DealerSeat,
                game.FirstPlayerSeat,
                game
            );
            await _eventPublisher.PublishAsync(handStartedEvent, cancellationToken);
            
            _logger.LogDebug("Started new hand {HandNumber} in game {GameId}", game.CurrentHand, gameId);
        }

        #region Helper Methods

        /// <summary>
        /// Clear all state related to the completed hand
        /// </summary>
        private static void ClearHandState(GameState game)
        {
            // Clear all played cards completely
            game.PlayedCards.Clear();
            
            // Clear round winners
            game.RoundWinners.Clear();
            
            // Reset round counter
            game.CurrentRound = TrucoConstants.Game.FirstRound;
              // Reset stakes and truco state
            game.Stakes = TrucoConstants.Stakes.Initial;
            game.TrucoCallState = TrucoCallState.None;
            game.LastTrucoCallerTeam = -1;
            game.CanRaiseTeam = null;
            game.IsBothTeamsAt10 = false;
            
            // Clear all player hands
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
                player.IsActive = false;
            }
        }

        /// <summary>
        /// Get the winning team
        /// </summary>
        private static Team? GetWinningTeam(GameState game)
        {
            return game.TeamScores.FirstOrDefault(kvp => kvp.Value >= TrucoConstants.Game.WinningScore).Key;
        }

        /// <summary>
        /// Rotate the dealer to the next player for the new hand
        /// </summary>
        private static void RotateDealer(GameState game)
        {
            // Update dealer - FirstPlayerSeat will be computed automatically
            game.DealerSeat = GameConfiguration.GetNextDealerSeat(game.DealerSeat);
            
            // Update player states
            foreach (var player in game.Players)
            {
                player.IsDealer = player.Seat == game.DealerSeat;
                player.IsActive = player.Seat == game.FirstPlayerSeat;
            }
            
            // Update current player index
            game.CurrentPlayerIndex = game.FirstPlayerSeat;
        }

        /// <summary>
        /// Deal new cards to all players
        /// </summary>
        private static void DealNewCards(GameState game)
        {
            // Create new shuffled deck
            game.Deck = new Deck();
            game.Deck.Shuffle();
            
            // Deal cards to all players
            game.DealCards();
        }

        /// <summary>
        /// Initialize played cards slots for the new hand
        /// </summary>
        private static void InitializePlayedCardsSlots(GameState game)
        {
            game.PlayedCards.Clear();
            for (int seat = 0; seat < TrucoConstants.Game.MaxPlayers; seat++)
            {
                game.PlayedCards.Add(new PlayedCard(seat));
            }
        }

        /// <summary>
        /// Get hand resolution delay using configuration values with fallback to defaults
        /// </summary>
        private TimeSpan GetHandResolutionDelay()
        {
            var delayMs = _configuration.GetValue<int>("GameSettings:HandResolutionDelayMs", GameConfiguration.DefaultHandResolutionDelayMs);
            
            if (delayMs <= 0)
            {
                return TimeSpan.Zero;
            }
            
            return TimeSpan.FromMilliseconds(delayMs);
        }

        #endregion
    }
}
