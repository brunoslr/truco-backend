using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Threading;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for managing Truco game logic (legacy wrapper around domain services)
    /// </summary>
    public class GameService
    {
        private readonly IGameStateManager _gameStateManager;
        private readonly IGameRepository _gameRepository;        private readonly IHandResolutionService _handResolutionService;
        private readonly ITrucoRulesEngine _trucoRulesEngine;
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IScoreCalculationService _scoreCalculationService;
        private readonly bool _devMode;
        private readonly int _aiPlayDelayMs;
        private readonly int _newHandDelayMs;

        /// <summary>
        /// Constructor for GameService
        /// </summary>
        /// <param name="gameStateManager">Game state management service</param>
        /// <param name="gameRepository">Game repository</param>
        /// <param name="handResolutionService">Hand resolution service</param>
        /// <param name="trucoRulesEngine">Truco rules engine</param>
        /// <param name="aiPlayerService">AI player service</param>
        /// <param name="scoreCalculationService">Score calculation service</param>
        /// <param name="configuration">Application configuration</param>
        public GameService(
            IGameStateManager gameStateManager,
            IGameRepository gameRepository,
            IHandResolutionService handResolutionService,
            ITrucoRulesEngine trucoRulesEngine,
            IAIPlayerService aiPlayerService,
            IScoreCalculationService scoreCalculationService,
            IConfiguration configuration)
        {
            _gameStateManager = gameStateManager;
            _gameRepository = gameRepository;
            _handResolutionService = handResolutionService;
            _trucoRulesEngine = trucoRulesEngine;            _aiPlayerService = aiPlayerService;
            _scoreCalculationService = scoreCalculationService;
              // Read configuration from appsettings.json
            _devMode = configuration.GetValue<bool>("FeatureFlags:DevMode", false);
            _aiPlayDelayMs = configuration.GetValue<int>("GameSettings:AIPlayDelayMs", GameConfiguration.DefaultAIPlayDelayMs);
            _newHandDelayMs = configuration.GetValue<int>("GameSettings:NewHandDelayMs", GameConfiguration.DefaultNewHandDelayMs);
        }        /// <summary>
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
        /// Plays a card from a player's hand
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player making the move (0-3)</param>
        /// <param name="cardIndex">The index of the card in the player's hand</param>
        /// <returns>True if the card was played successfully, false otherwise</returns>
        public bool PlayCard(string gameId, int playerSeat, int cardIndex)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null || !player.IsActive)
            {
                return false;
            }

            if (cardIndex < 0 || cardIndex >= player.Hand.Count)
            {
                return false;
            }

            // Play the card
            var card = player.PlayCard(cardIndex);
              // Find the player's played card slot
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
            MoveToNextPlayer(game);

            // Check if all players have played a card in this round
            if (game.PlayedCards.All(pc => pc.Card != null))
            {
                DetermineRoundWinner(game);
            }

            return true;
        }        /// <summary>
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
            });

            // Add a new hand result to the log
            game.ActionLog.Add(new ActionLogEntry("hand-result")
            {
                HandNumber = game.CurrentHand,
                Winner = opposingTeam,
                WinnerTeam = opposingTeam
            });

            // Reset for the next hand
            ResetForNewHand(game);
            
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
            ResetForNewHand(game);

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                Action = $"Started hand {game.CurrentHand}"
            });

            return true;
        }

        /// <summary>
        /// Resets the game state for a new hand
        /// </summary>
        /// <param name="game">The game state to reset</param>
        private void ResetForNewHand(GameState game)
        {
            // Increment hand number
            game.CurrentHand++;
              // Reset stakes and flags
            game.Stakes = TrucoConstants.Stakes.Initial;
            game.IsTrucoCalled = false;
            game.IsRaiseEnabled = true;
            
            // Rotate the dealer
            int currentDealerIndex = game.Players.FindIndex(p => p.IsDealer);
            int newDealerIndex = (currentDealerIndex + 1) % game.Players.Count;
            
            foreach (var player in game.Players)
            {
                player.IsDealer = false;
                player.IsActive = false;
                player.Hand.Clear();
            }
            
            game.Players[newDealerIndex].IsDealer = true;
            
            // Set the first player (left of dealer)
            int firstPlayerIndex = (newDealerIndex + 1) % game.Players.Count;
            game.Players[firstPlayerIndex].IsActive = true;
            game.FirstPlayerSeat = firstPlayerIndex;

            // Deal new cards
            var deck = new Deck();
            deck.Shuffle();
            foreach (var player in game.Players)
            {
                for (int i = 0; i < 3; i++)
                {
                    player.Hand.Add(deck.DrawCard());
                }
            }
              // Reset played cards
            game.PlayedCards.Clear();
            for (int i = 0; i < game.Players.Count; i++)
            {
                game.PlayedCards.Add(new PlayedCard(game.Players[i].Seat));
            }
        }        /// <summary>
        /// Move to the next player's turn (player to the left)
        /// </summary>
        private void MoveToNextPlayer(GameState game)
        {
            // Find the current active player
            var currentActivePlayer = game.Players.FirstOrDefault(p => p.IsActive);
            if (currentActivePlayer == null)
            {
                // If no active player, set the first player (left of dealer) as active
                var firstPlayer = game.Players.FirstOrDefault(p => p.Seat == game.FirstPlayerSeat);
                if (firstPlayer != null)
                {
                    firstPlayer.IsActive = true;
                    game.CurrentPlayerIndex = firstPlayer.Seat;
                }
                return;
            }

            // Set the current player to inactive
            currentActivePlayer.IsActive = false;

            // Find the next player (to the left, which is next seat in clockwise order)
            var nextPlayerSeat = (currentActivePlayer.Seat + 1) % GameConfiguration.MaxPlayers;
            var nextPlayer = game.Players.FirstOrDefault(p => p.Seat == nextPlayerSeat);

            // Set the next player to active
            if (nextPlayer != null)
            {
                nextPlayer.IsActive = true;
                game.CurrentPlayerIndex = nextPlayer.Seat;
            }
        }

        /// <summary>
        /// Determine the winner of the current round
        /// </summary>
        private void DetermineRoundWinner(GameState game)
        {
            // In a real implementation, this would use Truco card ranking rules
            // For now, we'll just use a simple value comparison for demonstration

            // Define card strength (Truco Mineiro uses different card strengths than traditional decks)
            var cardStrength = new Dictionary<string, int>
            {
                { "4", 1 }, { "5", 2 }, { "6", 3 }, { "7", 4 },
                { "Q", 5 }, { "J", 6 }, { "K", 7 }, { "A", 8 },
                { "2", 9 }, { "3", 10 }
            };

            var strongestCard = -1;
            Player? roundWinner = null;            foreach (var playedCard in game.PlayedCards)
            {
                if (playedCard.Card != null)
                {
                    var player = game.Players.FirstOrDefault(p => p.Seat == playedCard.PlayerSeat);
                    if (player != null)
                    {
                        var cardValue = cardStrength.GetValueOrDefault(playedCard.Card.Value, 0);
                        if (cardValue > strongestCard)
                        {
                            strongestCard = cardValue;
                            roundWinner = player;
                        }
                    }
                }
            }

            if (roundWinner != null)
            {
                // Set the winner and add to log
                game.TurnWinner = roundWinner.Team;
                game.ActionLog.Add(new ActionLogEntry("turn-result")
                {
                    Winner = roundWinner.Name,
                    WinnerTeam = roundWinner.Team
                });

                // Add the points to the winner's team
                game.AddScore(roundWinner.Team, game.Stakes);

                // Clear the played cards for the next round or hand
                if (game.PlayedCards.Any(pc => pc.Card != null && game.Players.Any(p => p.Hand.Count == 0)))
                {
                    // If any player has no cards left, move to the next hand
                    game.NextHand();
                }
                else
                {
                    // Otherwise, clear for the next round in the same hand
                    foreach (var pc in game.PlayedCards)
                    {
                        pc.Card = null;
                    }
                }
            }
        }        /// <summary>
        /// Enhanced play card method that handles human players, AI players, and fold scenarios
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">The seat of the player making the move (0-3)</param>
        /// <param name="cardIndex">The index of the card in the player's hand</param>
        /// <param name="isFold">Whether this is a fold action</param>
        /// <param name="requestingPlayerSeat">The seat of the player making the request (for response visibility)</param>
        /// <returns>PlayCardResponseDto with the updated game state</returns>
        public PlayCardResponseDto PlayCardEnhanced(string gameId, int playerSeat, int cardIndex, bool isFold = false, int requestingPlayerSeat = 0)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return MappingService.MapGameStateToPlayCardResponse(new GameState(), requestingPlayerSeat, _devMode, false, "Game not found");
            }

            // Handle fold action
            if (isFold)
            {
                var foldSuccess = Fold(gameId, playerSeat);
                if (!foldSuccess)
                {
                    return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Cannot fold at this time");
                }
                return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, true, "Hand folded successfully");
            }            // Handle card play for human player
            var playSuccess = PlayCard(gameId, playerSeat, cardIndex);
            if (!playSuccess)
            {
                return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Invalid card play");
            }

            // Check if we need to delay before starting a new hand
            bool isHandComplete = game.PlayedCards.All(pc => pc.Card != null);
            if (isHandComplete)
            {
                // Add delay before starting a new hand (will be applied on client side)
                Thread.Sleep(_newHandDelayMs);
            }

            // Automatically handle AI player turns
            HandleAIPlayerTurns(game);

            return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, true, "Card played successfully");
        }

        /// <summary>
        /// Handle AI player turns automatically in DevMode
        /// </summary>
        /// <param name="game">The game state</param>
        private void HandleAIPlayerTurns(GameState game)
        {
            // Continue playing for AI players until it's a human player's turn or round is complete
            var maxIterations = 10; // Prevent infinite loops
            var iterations = 0;

            while (iterations < maxIterations)
            {
                var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                if (activePlayer == null) break;

                // Check if it's a human player (seat 0 typically) or if round is complete
                if (activePlayer.Seat == 0 || game.PlayedCards.All(pc => pc.Card != null))
                {
                    break;
                }                // Add a delay before AI plays
                Thread.Sleep(_aiPlayDelayMs);
                
                // AI player plays first available card
                if (activePlayer.Hand.Count > 0)
                {
                    PlayCard(game.GameId, activePlayer.Seat, 0);
                }

                iterations++;
            }
        }
    }
}
