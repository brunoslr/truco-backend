using TrucoMineiro.API.Constants;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;

namespace TrucoMineiro.API.Services
{    /// <summary>
     /// Service for managing Truco game logic (legacy wrapper around domain services)
     /// </summary>
    public class GameService
    {        private readonly IGameStateManager _gameStateManager;
        private readonly IGameRepository _gameRepository;
        private readonly IGameFlowService _gameFlowService;
        private readonly ITrucoRulesEngine _trucoRulesEngine;
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IScoreCalculationService _scoreCalculationService;
        private readonly IEventPublisher _eventPublisher;


        private readonly bool _devMode;
        private readonly bool _autoAiPlay;
        private readonly int _aiPlayDelayMs;
        private readonly int _newHandDelayMs;        /// <summary>
        /// Constructor for GameService
        /// </summary>
        /// <param name="gameStateManager">Game state management service</param>
        /// <param name="gameRepository">Game repository</param>
        /// <param name="gameFlowService">Game flow service</param>
        /// <param name="trucoRulesEngine">Truco rules engine</param>
        /// <param name="aiPlayerService">AI player service</param>
        /// <param name="scoreCalculationService">Score calculation service</param>
        /// <param name="eventPublisher">Event publisher service</param>
        /// <param name="configuration">Application configuration</param>
        public GameService(
            IGameStateManager gameStateManager,
            IGameRepository gameRepository,
            IGameFlowService gameFlowService,
            ITrucoRulesEngine trucoRulesEngine,
            IAIPlayerService aiPlayerService,
            IScoreCalculationService scoreCalculationService,
            IEventPublisher eventPublisher,
            IConfiguration configuration)        {
            _gameStateManager = gameStateManager;            _gameRepository = gameRepository;
            _gameFlowService = gameFlowService;
            _trucoRulesEngine = trucoRulesEngine;
            _aiPlayerService = aiPlayerService;
            _scoreCalculationService = scoreCalculationService;
            _eventPublisher = eventPublisher;            // Read configuration from appsettings.json
            _devMode = configuration.GetValue<bool>("FeatureFlags:DevMode", false);
            _autoAiPlay = configuration.GetValue<bool>("FeatureFlags:AutoAiPlay", true);
            _aiPlayDelayMs = configuration.GetValue<int>("GameSettings:AIPlayDelayMs", GameConfiguration.DefaultMaxAIPlayDelayMs);
            _newHandDelayMs = configuration.GetValue<int>("GameSettings:NewHandDelayMs", GameConfiguration.DefaultNewHandDelayMs);
        }

        /// <summary>
        /// Creates a new game with a custom player name
        /// </summary>
        /// <param name="playerName">Name for the player at seat 0</param>
        /// <returns>The newly created game state</returns>
        public GameState CreateGame(string? playerName = null)
        {
            var gameTask = _gameStateManager.CreateGameAsync(playerName);
            var gameState = gameTask.GetAwaiter().GetResult();

            // Update stakes to 2 as per legacy requirements
            gameState.CurrentStake = 2;

            // Save the updated game state
            var saveTask = _gameRepository.SaveGameAsync(gameState);
            saveTask.GetAwaiter().GetResult();

            return gameState;
        }

        /// <summary>
        /// Checks if the development mode is enabled
        /// </summary>
        /// <returns>True if DevMode is enabled, false otherwise</returns>
        public bool IsDevMode()
        {
            return _devMode;
        }

        /// <summary>
        /// Gets a game by ID
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The game state if found, null otherwise</returns>
        public GameState? GetGame(string gameId)
        {
            var gameTask = _gameRepository.GetGameAsync(gameId);
            return gameTask.GetAwaiter().GetResult();
        }

        /// <summary>
        /// Plays a card from a player's hand with simplified logic
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player making the move (0-3)</param>
        /// <param name="cardIndex">The index of the card in the player's hand</param>
        /// <param name="isFold">Whether this is a fold action</param>
        /// <param name="requestingPlayerSeat">The seat of the player making the request (for response visibility)</param>
        /// <returns>PlayCardResponseDto with the updated game state</returns>
        public PlayCardResponseDto PlayCard(string gameId, int playerSeat, int cardIndex, bool isFold = false, int requestingPlayerSeat = 0)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return MappingService.MapGameStateToPlayCardResponse(new GameState(), requestingPlayerSeat, _devMode, false, "Game not found");
            }            // Handle fold action with special card creation (value=0, empty suit)
            if (isFold)
            {
                var foldSuccess = HandleFoldAction(game, playerSeat);
                if (!foldSuccess)
                {
                    return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Cannot fold at this time");
                }                // Publish fold event
                var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
                if (player != null)
                {
                    var foldCard = Card.CreateFoldCard();                    _ = Task.Run(async () => await _eventPublisher.PublishAsync(new CardPlayedEvent(
                        Guid.Parse(gameId),
                        player.Id,  // Already a Guid
                        foldCard,
                        player,
                        game.CurrentRound,
                        game.CurrentHand,
                        player.IsAI,
                        game
                    )));
                }
            }
            else
            {
                // Handle regular card play - simplified to only remove card and add to played cards
                var playSuccess = _gameFlowService.PlayCard(game, playerSeat, cardIndex);
                if (!playSuccess)
                {
                    return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Invalid card play");
                }

                // Publish card played event
                var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
                var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == playerSeat);
                if (player != null && playedCard?.Card != null)
                {                    _ = Task.Run(async () => await _eventPublisher.PublishAsync(new CardPlayedEvent(
                        Guid.Parse(gameId),
                        player.Id,  // Already a Guid
                        playedCard.Card,
                        player,
                        game.CurrentRound,
                        game.CurrentHand,
                        player.IsAI,
                        game
                    )));
                }            }            // NOTE: Post-card-play reactions are now handled by the event-driven architecture:
            // 1. CardPlayedEvent is published above
            // 2. GameFlowEventHandler processes the event and advances to next player
            // 3. PlayerTurnStartedEvent is published for the next player
            // 4. AIPlayerEventHandler processes AI turns automatically
            // 5. Round/Hand completion is handled by respective event handlers
            //
            // OLD APPROACH (REMOVED): Previously, we called ProcessAITurnsAsync() and ProcessHandCompletionAsync() 
            // synchronously here, but this caused race conditions and inconsistencies with the event-driven flow.
            
            // Save the game state (events will be processed asynchronously)
            var saveTask = _gameRepository.SaveGameAsync(game);
            saveTask.GetAwaiter().GetResult();

            return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, true, "Card played successfully");
        }

        /// <summary>
        /// Handles fold action by creating a special card with value=0 and empty suit
        /// </summary>
        private bool HandleFoldAction(GameState game, int playerSeat)
        {
            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null || !player.IsActive)
            {
                return false;
            }            // Create fold card using proper method
            var foldCard = Card.CreateFoldCard();// Find the player's played card slot and set the fold card
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
            if (playedCard != null)
            {
                playedCard.Card = foldCard;
            }
            else
            {
                game.PlayedCards.Add(new PlayedCard(player.Seat, foldCard));
            }

            // ActionLog entry will be created by ActionLogEventHandler when CardPlayedEvent is published

            // Move to the next player's turn
            _gameFlowService.AdvanceToNextPlayer(game);

            return true;
        }

        /// <summary>
        /// Call Truco to raise the stakes
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player calling Truco (0-3)</param>
        /// <returns>True if the call was successful, false otherwise</returns>
        public bool CallTruco(string gameId, int playerSeat)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
            {
                return false;
            }            // Check if raising is allowed
            if (!game.IsRaiseEnabled || game.Stakes >= TrucoConstants.Stakes.Maximum)
            {
                return false;
            }

            // Calculate the new stakes
            int newStakes;
            if (!game.IsTrucoCalled)
            {
                // First Truco call
                newStakes = TrucoConstants.Stakes.TrucoCall;
                game.IsTrucoCalled = true;
            }
            else
            {
                // Raise stakes: 4 -> 8 -> 12 (each raise adds 4)
                newStakes = game.Stakes + TrucoConstants.Stakes.RaiseAmount; if (newStakes > TrucoConstants.Stakes.Maximum)
                {
                    return false;
                }            }

            game.Stakes = newStakes;

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerSeat = player.Seat,
                Action = $"Raised stakes to {newStakes}"
            });            // Publish TrucoRaiseEvent
            var trucoRaiseEvent = new TrucoRaiseEvent(
                Guid.Parse(gameId),
                player.Id,  // Already a Guid
                player,
                game.Stakes - (newStakes - game.Stakes), // currentStakes before the change
                newStakes,
                !game.IsTrucoCalled, // isInitialTruco
                game
            );
            _ = Task.Run(async () => await _eventPublisher.PublishAsync(trucoRaiseEvent));

            return true;
        }        
        
        /// <summary>
        /// Raises the stakes after a Truco call
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player raising the stakes (0-3)</param>
        /// <returns>True if the stakes were raised successfully, false otherwise</returns>
        public bool RaiseStakes(string gameId, int playerSeat)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
            {
                return false;
            }            // Check if we can raise (Truco must have been called)
            if (!game.IsTrucoCalled || !game.IsRaiseEnabled || game.Stakes >= TrucoConstants.Stakes.Maximum)
            {
                return false;
            }

            // Calculate the new stakes (each raise adds 4)
            int newStakes = game.Stakes + TrucoConstants.Stakes.RaiseAmount;
            if (newStakes > TrucoConstants.Stakes.Maximum)
            {
                return false;
            }            game.Stakes = newStakes;

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerSeat = player.Seat,
                Action = $"Raised stakes to {newStakes}"
            });            // Publish TrucoRaiseEvent
            var trucoRaiseEvent = new TrucoRaiseEvent(
                Guid.Parse(gameId),
                player.Id,  // Already a Guid
                player,
                game.Stakes - TrucoConstants.Stakes.RaiseAmount, // currentStakes before the raise
                newStakes,
                false, // isInitialTruco (this is always a raise, not initial truco)
                game
            );
            _ = Task.Run(async () => await _eventPublisher.PublishAsync(trucoRaiseEvent));

            return true;
        }        
        /// <summary>
        /// Folds the hand in response to a Truco or other challenge
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player folding (0-3)</param>
        /// <returns>True if the fold was successful, false otherwise</returns>
        public bool Fold(string gameId, int playerSeat)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
            {
                return false;
            }

            // Get opposing team
            string opposingTeam = player.Team == "Player's Team" ? "Opponent Team" : "Player's Team";

            // Award points to the opposing team
            game.TeamScores[opposingTeam] += Math.Max(1, game.Stakes);            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerSeat = player.Seat,
                Action = $"Folded, {opposingTeam} gains {game.Stakes} points"
            });            // Add a new hand result to the log
            game.ActionLog.Add(new ActionLogEntry("hand-result")
            {
                HandNumber = game.CurrentHand,
                Winner = opposingTeam,
                WinnerTeam = opposingTeam
            });            // Publish FoldHandEvent
            var foldHandEvent = new FoldHandEvent(
                Guid.Parse(gameId),
                player.Id,  // Already a Guid
                player,
                game.CurrentHand,
                game.Stakes,
                opposingTeam,
                game
            );
            _ = Task.Run(async () => await _eventPublisher.PublishAsync(foldHandEvent));

            // Reset for the next hand
            _gameFlowService.StartNewHand(game);

            return true;
        }

        /// <summary>
        /// Starts a new hand in the current game
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>True if the new hand was started successfully, false otherwise</returns>
        public bool StartNewHand(string gameId)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            // Reset the game for a new hand
            _gameFlowService.StartNewHand(game);

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                Action = $"Started hand {game.CurrentHand}"
            });

            return true;
        }
    }
}
