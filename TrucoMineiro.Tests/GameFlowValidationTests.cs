using Microsoft.Extensions.Configuration;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using Moq;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for validating game flow and state consistency
    /// </summary>
    public class GameFlowValidationTests
    {
        private readonly IConfiguration _configuration;

        public GameFlowValidationTests()
        {
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
                {"FeatureFlags:AutoAiPlay", "false"}, // Disable auto AI play for manual control
                {"GameSettings:AIPlayDelayMs", "0"},
                {"GameSettings:NewHandDelayMs", "0"}
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        private GameService CreateGameService()
        {
            // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();
              // Create mock services
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockGameFlowService = new Mock<IGameFlowService>();
            var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
            var mockAIPlayerService = new Mock<IAIPlayerService>();
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();
            var mockEventPublisher = new Mock<IEventPublisher>();

            // Configure mock GameStateManager to return a valid GameState and store it
            mockGameStateManager.Setup(x => x.CreateGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string playerName) => {
                    var gameState = CreateValidGameState(playerName);
                    gameStorage[gameState.GameId] = gameState;
                    return gameState;
                });

            // Configure mock GameRepository to return games from storage and update storage when saved
            mockGameRepository.Setup(x => x.GetGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);

            mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
                .ReturnsAsync((GameState gameState) => {
                    gameStorage[gameState.GameId] = gameState;
                    return true;
                });

            // Configure mock ScoreCalculationService
            mockScoreCalculationService.Setup(x => x.IsGameComplete(It.IsAny<GameState>()))
                .Returns(false);

            // Configure mock TrucoRulesEngine
            mockTrucoRulesEngine.Setup(x => x.CalculateHandPoints(It.IsAny<GameState>()))
                .Returns(1);            // Configure mock GameFlowService to simulate real card playing
            mockGameFlowService.Setup(x => x.PlayCard(It.IsAny<GameState>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((GameState gameState, int playerSeat, int cardIndex) => {
                    var player = gameState.Players[playerSeat];
                    if (cardIndex < 0 || cardIndex >= player.Hand.Count)
                        return false;
                    
                    // Store the card before removing it
                    var cardToPlay = player.Hand[cardIndex];
                    
                    // Remove card from player's hand
                    player.Hand.RemoveAt(cardIndex);
                    
                    // Add to played cards
                    gameState.PlayedCards.Add(new PlayedCard(playerSeat, cardToPlay));
                    
                    // Move to next player
                    gameState.CurrentPlayerIndex = (playerSeat + 1) % 4;
                    
                    // Update player activity flags
                    foreach (var p in gameState.Players)
                    {
                        p.IsActive = (p.Seat == gameState.CurrentPlayerIndex);
                    }
                    
                    return true;
                });

            // Configure ProcessHandCompletionAsync to do nothing (no hand completion logic needed for this test)
            mockGameFlowService.Setup(x => x.ProcessHandCompletionAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);            // Configure ProcessAITurnsAsync to do nothing (AutoAiPlay is disabled)
            mockGameFlowService.Setup(x => x.ProcessAITurnsAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);
                  return new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                mockEventPublisher.Object,
                _configuration);
        }

        private GameState CreateValidGameState(string? playerName = null)
        {
            var gameState = new GameState();
            gameState.InitializeGame(playerName ?? "TestPlayer");
            gameState.FirstPlayerSeat = 0; // Ensure human player starts
            gameState.CurrentPlayerIndex = 0; // Ensure human player is active
            return gameState;
        }        [Fact]
        public void GameFlow_ShouldReflectPlayedCardsInGameState()
        {
            // Step 1: Create a new game
            var gameService = CreateGameService();
            var game = gameService.CreateGame("TestPlayer");
            
            // Verify initial state
            Assert.Equal(0, game.CurrentPlayerIndex); // First player should be active
            Assert.Equal(4, game.Players.Count); // Should have 4 players
            Assert.All(game.Players, player => Assert.Equal(3, player.Hand.Count)); // Each player should have 3 cards
            Assert.Empty(game.PlayedCards); // No cards should be played yet

            // Ensure player 0 is active (needed for HandleCardPlay check in PlayCard)
            game.CurrentPlayerIndex = 0;
            var activePlayer = game.Players.FirstOrDefault(p => p.Seat == 0);
            if (activePlayer != null)
            {
                activePlayer.IsActive = true;
                foreach (var player in game.Players.Where(p => p.Seat != 0))
                {
                    player.IsActive = false;
                }
            }

            // Step 2: Have first player (seat 0) play the first card - save the value for later check
            var firstPlayer = game.Players[0];
            var firstCardToPlay = firstPlayer.Hand[0]; // Save the card that will be played
            var firstCardValue = firstCardToPlay.Value;
            var firstCardSuit = firstCardToPlay.Suit;
            
            Console.WriteLine($"First player will play: {firstCardValue} of {firstCardSuit}");
            var firstPlayResponse = gameService.PlayCard(game.GameId, 0, 0, false, 0);
            
            // Verify first play was successful
            Assert.True(firstPlayResponse.Success, $"First play failed: {firstPlayResponse.Message}");
            
            // Step 3: Have next player (seat 1) play a card
            var updatedGame = gameService.GetGame(game.GameId);
            Assert.NotNull(updatedGame);
            Assert.Equal(1, updatedGame.CurrentPlayerIndex); // Should be second player's turn
            
            var secondPlayer = updatedGame.Players[1];
            var secondCardToPlay = secondPlayer.Hand[0]; // Save the card that will be played
            var secondCardValue = secondCardToPlay.Value;
            var secondCardSuit = secondCardToPlay.Suit;
            
            Console.WriteLine($"Second player will play: {secondCardValue} of {secondCardSuit}");
              var secondPlayResponse = gameService.PlayCard(game.GameId, 1, 0, false, 0);
            
            // Verify second play was successful
            Assert.True(secondPlayResponse.Success, $"Second play failed: {secondPlayResponse.Message}");
            
            // Step 4: Call get game state and check current state
            var finalGameState = gameService.GetGame(game.GameId);
            Assert.NotNull(finalGameState);
            
            Console.WriteLine($"Final game state - Current player: {finalGameState.CurrentPlayerIndex}");
            Console.WriteLine($"Played cards count: {finalGameState.PlayedCards.Count}");
            
            foreach (var player in finalGameState.Players)
            {
                Console.WriteLine($"Player {player.Seat}: {player.Hand.Count} cards in hand");
            }
              foreach (var playedCard in finalGameState.PlayedCards)
            {
                Console.WriteLine($"Played card - Player {playedCard.PlayerSeat}: {playedCard.Card!.Value} of {playedCard.Card.Suit}");
            }
            
            // Verify hand counts: Players who played should have 2 cards, others should have 3
            Assert.Equal(2, finalGameState.Players[0].Hand.Count); // First player should have 2 cards
            Assert.Equal(2, finalGameState.Players[1].Hand.Count); // Second player should have 2 cards  
            Assert.Equal(3, finalGameState.Players[2].Hand.Count); // Third player should have 3 cards
            Assert.Equal(3, finalGameState.Players[3].Hand.Count); // Fourth player should have 3 cards
            
            // Verify played cards are visible in PlayedCards array
            Assert.Equal(2, finalGameState.PlayedCards.Count); // Should have 2 played cards
              // Verify first played card matches what was played
            var firstPlayedCard = finalGameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == 0);
            Assert.NotNull(firstPlayedCard);
            Assert.NotNull(firstPlayedCard.Card);
            Assert.Equal(firstCardValue, firstPlayedCard.Card!.Value);
            Assert.Equal(firstCardSuit, firstPlayedCard.Card.Suit);
            
            // Verify second played card matches what was played
            var secondPlayedCard = finalGameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == 1);
            Assert.NotNull(secondPlayedCard);
            Assert.NotNull(secondPlayedCard.Card);
            Assert.Equal(secondCardValue, secondPlayedCard.Card!.Value);
            Assert.Equal(secondCardSuit, secondPlayedCard.Card.Suit);
        }
    }
}
