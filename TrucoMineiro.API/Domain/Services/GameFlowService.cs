using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using Microsoft.Extensions.Logging;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service responsible for game flow orchestration and event generation
    /// Handles the business logic of what happens after game events
    /// </summary>
    public class GameFlowService : IGameFlowService
    {
        private readonly IGameStateManager _gameStateManager;
        private readonly IHandResolutionService _handResolutionService;
        private readonly ITrucoRulesEngine _trucoRulesEngine;
        private readonly ILogger<GameFlowService> _logger;

        public GameFlowService(
            IGameStateManager gameStateManager,
            IHandResolutionService handResolutionService,
            ITrucoRulesEngine trucoRulesEngine,
            ILogger<GameFlowService> logger)
        {
            _gameStateManager = gameStateManager;
            _handResolutionService = handResolutionService;
            _trucoRulesEngine = trucoRulesEngine;
            _logger = logger;
        }        /// <summary>
        /// Processes game flow after a card is played and returns events to publish
        /// </summary>
        public List<object> ProcessCardPlayedFlow(GameState game)
        {
            var events = new List<object>();
            
            // Advance to next player
            _gameStateManager.AdvanceToNextPlayer(game);

            if (_gameStateManager.IsRoundComplete(game))
            {
                var winner = DetermineRoundWinner(game);
                
                // Create round completed event
                var playedCards = game.PlayedCards.Where(pc => pc.Card != null && !pc.Card.IsEmpty).Select(pc => pc.Card!).ToList();
                var roundCompletedEvent = new RoundCompletedEvent(
                    Guid.Parse(game.Id),
                    game.CurrentRound,
                    game.CurrentHand,
                    winner,
                    playedCards,
                    new Dictionary<Guid, int>(), // Score changes
                    game,
                    winner == null // isDraw
                );
                events.Add(roundCompletedEvent);

                if (IsHandComplete(game))
                {
                    // Hand completion logic would go here
                    _logger.LogInformation("Hand completed in game {GameId} - Hand completion logic needed", game.Id);
                    _gameStateManager.StartNewHand(game);
                }
                else
                {
                    // Start new round
                    ClearPlayedCards(game);
                    var nextPlayer = winner ?? game.Players.First();
                    SetActivePlayer(game, nextPlayer);
                    
                    var nextTurnEvent = new PlayerTurnStartedEvent(
                        Guid.Parse(game.Id),
                        nextPlayer,
                        game.CurrentRound,
                        game.CurrentHand,
                        game,
                        new List<string> { "play-card" }
                    );
                    events.Add(nextTurnEvent);
                }
            }
            else
            {
                // Continue with next player
                var activePlayer = GetActivePlayer(game);
                if (activePlayer != null)
                {
                    var nextTurnEvent = new PlayerTurnStartedEvent(
                        Guid.Parse(game.Id),
                        activePlayer,
                        game.CurrentRound,
                        game.CurrentHand,
                        game,
                        new List<string> { "play-card" }
                    );
                    events.Add(nextTurnEvent);
                }
            }

            return events;
        }

        /// <summary>
        /// Processes game flow after a truco/raise is called and returns events to publish
        /// </summary>
        public List<object> ProcessTrucoRaiseFlow(GameState game, Player callingPlayer, int newStakes)
        {
            var events = new List<object>();
            
            // Update game stakes
            game.Stakes = newStakes;

            // Find the next player who needs to respond
            var opposingPlayers = game.Players.Where(p => p.Seat != callingPlayer.Seat).ToList();
            var nextResponder = opposingPlayers.FirstOrDefault();
            
            if (nextResponder != null)
            {
                SetActivePlayer(game, nextResponder);

                var availableActions = new List<string> { "accept-truco", "fold", "raise-truco" };
                var nextTurnEvent = new PlayerTurnStartedEvent(
                    Guid.Parse(game.Id),
                    nextResponder,
                    game.CurrentRound,
                    game.CurrentHand,
                    game,
                    availableActions
                );
                events.Add(nextTurnEvent);
            }

            return events;
        }

        /// <summary>
        /// Processes game flow after a fold occurs and returns events to publish
        /// </summary>
        public List<object> ProcessFoldFlow(GameState game, Player foldingPlayer, string winningTeam, int currentStakes)
        {
            var events = new List<object>();
            
            // Award points to the winning team
            AwardPointsToTeam(game, winningTeam, currentStakes);

            // Check if game is complete
            if (IsGameComplete(game))
            {
                game.Status = GameStatus.Completed;
            }
            else
            {
                // Start new hand
                _gameStateManager.StartNewHand(game);
                ClearPlayedCards(game);

                // Set first player active for new hand
                var firstPlayer = game.Players.First();
                SetActivePlayer(game, firstPlayer);

                var nextTurnEvent = new PlayerTurnStartedEvent(
                    Guid.Parse(game.Id),
                    firstPlayer,
                    1, // New hand starts at round 1
                    game.CurrentHand,
                    game,
                    new List<string> { "play-card", "truco" }
                );
                events.Add(nextTurnEvent);
            }

            return events;
        }

        #region Private Helper Methods

        /// <summary>
        /// Determines the winner of a completed round using the hand resolution service
        /// </summary>
        private Player? DetermineRoundWinner(GameState game)
        {
            var playedCards = game.PlayedCards.Where(pc => pc.Card != null && !pc.Card.IsEmpty).ToList();
            return _handResolutionService.DetermineRoundWinner(playedCards, game.Players);
        }

        /// <summary>
        /// Checks if the current hand is complete (all cards played)
        /// </summary>
        private bool IsHandComplete(GameState game)
        {
            return game.Players.All(p => p.Hand.Count == 0);
        }

        /// <summary>
        /// Get the currently active player
        /// </summary>
        private Player? GetActivePlayer(GameState game)
        {
            return game.Players.FirstOrDefault(p => p.IsActive);
        }

        /// <summary>
        /// Clears played cards for a new round
        /// </summary>
        private void ClearPlayedCards(GameState game)
        {
            foreach (var pc in game.PlayedCards)
            {
                pc.Card = Card.CreateEmptyCard();
            }
        }

        /// <summary>
        /// Sets up the next player to be active
        /// </summary>
        private void SetActivePlayer(GameState game, Player player)
        {
            game.Players.ForEach(p => p.IsActive = false);
            player.IsActive = true;
            game.CurrentPlayerIndex = player.Seat;
        }

        /// <summary>
        /// Checks if the game is complete (reached winning score)
        /// </summary>
        private bool IsGameComplete(GameState game)
        {
            return game.Team1Score >= 12 || game.Team2Score >= 12;
        }

        /// <summary>
        /// Awards points to a team
        /// </summary>
        private void AwardPointsToTeam(GameState game, string team, int points)
        {
            if (team == "Team 1")
            {
                game.Team1Score += points;
            }
            else if (team == "Team 2")
            {
                game.Team2Score += points;
            }
        }

        #endregion
    }
}
