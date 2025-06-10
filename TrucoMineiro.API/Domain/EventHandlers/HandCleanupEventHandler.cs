using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for cleaning up game state after hands are completed
    /// </summary>
    public class HandCleanupEventHandler : IEventHandler<HandCompletedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;

        public HandCleanupEventHandler(IGameRepository gameRepository, IEventPublisher eventPublisher)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
        }

        /// <summary>
        /// Handle hand completed events and perform hand cleanup
        /// </summary>
        public async Task HandleAsync(HandCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = gameEvent.GameState;
            
            // Clear all hand-related state
            ClearHandState(game);
            
            // Prepare for next hand
            await PrepareNextHand(game, cancellationToken);
            
            // Save the updated game state
            await _gameRepository.SaveGameAsync(game);
        }

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
            game.IsTrucoCalled = false;
            game.IsRaiseEnabled = true;
            
            // Clear all player hands
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
                player.IsActive = false;
            }
        }        /// <summary>
        /// Prepare the game for the next hand if the game is not complete
        /// </summary>
        private async Task PrepareNextHand(GameState game, CancellationToken cancellationToken)
        {
            // Check if game is complete (one team reached winning score)
            if (IsGameComplete(game))
            {
                game.GameStatus = "completed";
                
                // Calculate game duration
                var gameDuration = DateTime.UtcNow - game.CreatedAt;
                
                // Get winning player (first player from winning team)
                var winningPlayer = GetWinningPlayer(game);
                  // Convert team scores to player scores dictionary
                var finalScores = new Dictionary<Guid, int>();
                foreach (var player in game.Players)
                {
                    var teamScore = game.TeamScores.ContainsKey(player.Team) ? game.TeamScores[player.Team] : 0;
                    finalScores[player.Id] = teamScore;  // player.Id is already a Guid
                }
                
                // Publish game completed event
                var gameCompletedEvent = new GameCompletedEvent(
                    Guid.Parse(game.GameId),
                    winningPlayer,
                    finalScores,
                    game,
                    gameDuration
                );
                await _eventPublisher.PublishAsync(gameCompletedEvent, cancellationToken);
                return;
            }
            
            // Move to next hand
            game.CurrentHand++;
            
            // Rotate dealer and set first player
            RotateDealer(game);
            
            // Deal new cards
            DealNewCards(game);
            
            // Initialize played cards slots for new hand
            InitializePlayedCardsSlots(game);
            
            // Publish hand started event
            var handStartedEvent = new HandStartedEvent(
                Guid.Parse(game.GameId),
                game.CurrentHand,
                game.DealerSeat,
                game.FirstPlayerSeat,
                game
            );
            await _eventPublisher.PublishAsync(handStartedEvent, cancellationToken);
        }

        /// <summary>
        /// Check if the game is complete (one team reached winning score)
        /// </summary>
        private static bool IsGameComplete(GameState game)
        {
            return game.TeamScores.Values.Any(score => score >= TrucoConstants.Game.WinningScore);
        }        /// <summary>
        /// Get the winning player (first player from the winning team)
        /// </summary>
        private static Player? GetWinningPlayer(GameState game)
        {
            var winningTeamEntry = game.TeamScores.FirstOrDefault(kvp => kvp.Value >= TrucoConstants.Game.WinningScore);
            if (string.IsNullOrEmpty(winningTeamEntry.Key))
                return null;
            
            // Return the first player from the winning team
            return game.Players.FirstOrDefault(p => p.Team == winningTeamEntry.Key);
        }

        /// <summary>
        /// Rotate the dealer to the next player for the new hand
        /// </summary>
        private static void RotateDealer(GameState game)
        {
            // Update dealer and first player
            game.DealerSeat = GameConfiguration.GetNextDealerSeat(game.DealerSeat);
            game.FirstPlayerSeat = GameConfiguration.GetFirstPlayerSeat(game.DealerSeat);
            
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
    }
}
