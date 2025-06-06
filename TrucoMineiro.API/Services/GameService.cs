using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading;

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
        private readonly IGameFlowReactionService _gameFlowReactionService;

        private readonly bool _devMode;
        private readonly bool _autoAiPlay;
        private readonly int _aiPlayDelayMs;
        private readonly int _newHandDelayMs;

        /// <summary>
        /// Constructor for GameService
        /// </summary>
        /// <param name="gameStateManager">Game state management service</param>
        /// <param name="gameRepository">Game repository</param>
        /// <param name="gameFlowService">Game flow service</param>
        /// <param name="trucoRulesEngine">Truco rules engine</param>
        /// <param name="aiPlayerService">AI player service</param>
        /// <param name="scoreCalculationService">Score calculation service</param>
        /// <param name="gameFlowReactionService">Game flow reaction service</param>
        /// <param name="configuration">Application configuration</param>
        public GameService(
            IGameStateManager gameStateManager,
            IGameRepository gameRepository,
            IGameFlowService gameFlowService,
            ITrucoRulesEngine trucoRulesEngine,
            IAIPlayerService aiPlayerService,
            IScoreCalculationService scoreCalculationService,
            IGameFlowReactionService gameFlowReactionService,
            IConfiguration configuration)
        {
            _gameStateManager = gameStateManager;
            _gameRepository = gameRepository;
            _gameFlowService = gameFlowService;
            _trucoRulesEngine = trucoRulesEngine;
            _aiPlayerService = aiPlayerService;
            _scoreCalculationService = scoreCalculationService;
            _gameFlowReactionService = gameFlowReactionService;
            
            // Read configuration from appsettings.json
            _devMode = configuration.GetValue<bool>("FeatureFlags:DevMode", false);
            _autoAiPlay = configuration.GetValue<bool>("FeatureFlags:AutoAiPlay", true);
            _aiPlayDelayMs = configuration.GetValue<int>("GameSettings:AIPlayDelayMs", GameConfiguration.DefaultAIPlayDelayMs);
            _newHandDelayMs = configuration.GetValue<int>("GameSettings:NewHandDelayMs", GameConfiguration.DefaultNewHandDelayMs);
        }/// <summary>
        /// Creates a new game with 4 players and deals cards
        /// </summary>
        /// <returns>The newly created game state</returns>
        public GameState CreateGame()
        {
            var gameTask = _gameStateManager.CreateGameAsync();
            var gameState = gameTask.GetAwaiter().GetResult();

            // In dev mode, the game is properly initialized with one active player
            // No need to override the proper active player logic
            
            // Save the updated game state
            var saveTask = _gameRepository.SaveGameAsync(gameState);
            saveTask.GetAwaiter().GetResult();

            return gameState;
        }

        /// <summary>
        /// Creates a new game with a custom player name
        /// </summary>
        /// <param name="playerName">Name for the player at seat 0</param>
        /// <returns>The newly created game state</returns>
        public GameState CreateGame(string playerName)
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
        }        /// <summary>
        /// Gets a game by ID
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The game state if found, null otherwise</returns>
        public GameState? GetGame(string gameId)
        {
            var gameTask = _gameRepository.GetGameAsync(gameId);
            return gameTask.GetAwaiter().GetResult();
        }        /// <summary>
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
            }

            // Handle fold action with special card creation (value=0, empty suit)
            if (isFold)
            {
                var foldSuccess = HandleFoldAction(game, playerSeat);
                if (!foldSuccess)
                {
                    return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Cannot fold at this time");
                }
            }
            else
            {
                // Handle regular card play - simplified to only remove card and add to played cards
                var playSuccess = HandleCardPlay(game, playerSeat, cardIndex);
                if (!playSuccess)
                {
                    return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Invalid card play");
                }
            }

            // Process post-card-play reactions using the new flow mechanism
            var reactionTask = _gameFlowReactionService.ProcessCardPlayReactionsAsync(game, _autoAiPlay, _aiPlayDelayMs, _newHandDelayMs);
            reactionTask.GetAwaiter().GetResult();

            // Save the game state
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
            }

            // Create special fold card with value=0 and empty suit
            var foldCard = new Card { Value = "0", Suit = "" };

            // Find the player's played card slot and set the fold card
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
            if (playedCard != null)
            {
                playedCard.Card = foldCard;
            }
            else
            {
                game.PlayedCards.Add(new PlayedCard(player.Seat, foldCard));
            }

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("card-played")
            {
                PlayerSeat = player.Seat,
                Card = "Fold"
            });

            // Move to the next player's turn
            _gameFlowService.AdvanceToNextPlayer(game);

            return true;
        }

        /// <summary>
        /// Handles regular card play by removing card from hand and adding to played cards
        /// </summary>
        private bool HandleCardPlay(GameState game, int playerSeat, int cardIndex)
        {
            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null || !player.IsActive)
            {
                return false;
            }

            if (cardIndex < 0 || cardIndex >= player.Hand.Count)
            {
                return false;
            }

            // Remove card from player's hand
            var card = player.Hand[cardIndex];
            player.Hand.RemoveAt(cardIndex);

            // Add to played cards array
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
            if (playedCard != null)
            {
                playedCard.Card = card;
            }
            else
            {
                game.PlayedCards.Add(new PlayedCard(player.Seat, card));
            }

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("card-played")
            {
                PlayerSeat = player.Seat,
                Card = $"{card.Value} of {card.Suit}"
            });

            // Move to the next player's turn
            _gameFlowService.AdvanceToNextPlayer(game);

            return true;
        }/// <summary>
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
            }            else
            {
                // Raise stakes: 4 -> 8 -> 12 (each raise adds 4)
                newStakes = game.Stakes + TrucoConstants.Stakes.RaiseAmount;            if (newStakes > TrucoConstants.Stakes.Maximum)
                {
                    return false;
                }
            }

            game.Stakes = newStakes;

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerSeat = player.Seat,
                Action = $"Raised stakes to {newStakes}"
            });

            return true;
        }        /// <summary>
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
            });

            return true;
        }        /// <summary>
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
            });

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
                return false;            }

            // Reset the game for a new hand
            _gameFlowService.StartNewHand(game);

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                Action = $"Started hand {game.CurrentHand}"
            });

            return true;
        }/// <summary>
        /// Enhanced play card method that handles human players, AI players, and fold scenarios
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player making the move (0-3)</param>
        /// <param name="cardIndex">The index of the card in the player's hand</param>
        /// <param name="isFold">Whether this is a fold action</param>
        /// <param name="requestingPlayerSeat">The seat of the player making the request (for response visibility)</param>
        /// <returns>PlayCardResponseDto with the updated game state</returns>        /// <summary>
        /// Enhanced play card method that now just forwards to the simplified PlayCard method
        /// This is kept for backwards compatibility with tests
        /// </summary>
        public PlayCardResponseDto PlayCardEnhanced(string gameId, int playerSeat, int cardIndex, bool isFold = false, int requestingPlayerSeat = 0)
        {
            // Simply forward to the new PlayCard method for consistency
            return PlayCard(gameId, playerSeat, cardIndex, isFold, requestingPlayerSeat);
        }
    }
}
