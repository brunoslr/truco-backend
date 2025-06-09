using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrucoMineiro.API.Controllers;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Services;
using Moq;
using Xunit;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests for the PlayCard endpoint with delays between AI plays and new hands
    /// </summary>
    public class PlayCardDelayTests
    {
        private readonly GameService _gameService;
        private readonly TrucoGameController _controller;

        public PlayCardDelayTests()
        {            // Set up test configuration with custom delay settings
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
                {"FeatureFlags:AutoAiPlay", "true"}, // Enable AI auto-play
                {"GameSettings:AIPlayDelayMs", "100"},  // Use a small delay for tests
                {"GameSettings:NewHandDelayMs", "200"}  // Use a small delay for tests
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();            // Create mock services
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockGameFlowService = new Mock<IGameFlowService>();
            var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
            var mockAIPlayerService = new Mock<IAIPlayerService>();
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();            // Configure mock GameStateManager to return a valid GameState and store it
            mockGameStateManager.Setup(x => x.CreateGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string playerName) => {
                    var gameState = new GameState();
                    gameState.InitializeGame(playerName);
                    // Fix the FirstPlayerSeat to match the real GameStateManager behavior
                    gameState.FirstPlayerSeat = 0; // Human player should be first
                    // Ensure human player (seat 0) is active, not AI (seat 1)
                    foreach (var player in gameState.Players)
                    {
                        player.IsActive = player.Seat == 0;
                    }
                    gameStorage[gameState.GameId] = gameState;
                    return gameState;
                });
            
            // Configure mock GameRepository to return games from storage
            mockGameRepository.Setup(x => x.GetGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);            mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
                .ReturnsAsync((GameState gameState) => {
                    gameStorage[gameState.GameId] = gameState;
                    return true;
                });

            // Configure mock GameFlowService to simulate real card playing behavior
            mockGameFlowService.Setup(x => x.PlayCard(It.IsAny<GameState>(), It.IsAny<int>(), It.IsAny<int>()))
                .Returns((GameState game, int playerSeat, int cardIndex) => {
                    var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
                    if (player == null || !player.IsActive || cardIndex < 0 || cardIndex >= player.Hand.Count)
                    {
                        return false;
                    }
                      // Play the card (simulate the real behavior)
                    var card = player.Hand[cardIndex];
                    player.Hand.RemoveAt(cardIndex);
                    
                    // Add to played cards
                    game.PlayedCards.Add(new PlayedCard(playerSeat, card));
                    
                    // Move to next player (simple rotation)
                    player.IsActive = false;
                    var nextPlayerSeat = (playerSeat + 1) % 4;
                    var nextPlayer = game.Players.FirstOrDefault(p => p.Seat == nextPlayerSeat);
                    if (nextPlayer != null)
                    {
                        nextPlayer.IsActive = true;
                    }
                    
                    return true;
                });            mockGameFlowService.Setup(x => x.ProcessAITurnsAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns((GameState gameState, int aiPlayDelayMs) => {
                    // Simulate AI players playing cards automatically
                    for (int seat = 1; seat <= 3; seat++) // AI players are seats 1, 2, 3
                    {
                        var aiPlayer = gameState.Players[seat];
                        if (aiPlayer.Hand.Count > 0)
                        {
                            var cardToPlay = aiPlayer.Hand[0];
                            aiPlayer.Hand.RemoveAt(0);
                            gameState.PlayedCards.Add(new PlayedCard(seat, cardToPlay));
                        }
                    }
                    return Task.CompletedTask;
                });            mockGameFlowService.Setup(x => x.ProcessHandCompletionAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);

            _gameService = new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                configuration);
            _controller = new TrucoGameController(_gameService);
        }        [Fact]
        public void PlayCard_ShouldUpdatePlayedCardsArray()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var cardIndex = 0;
            
            // Store the card that will be played for later verification
            var cardToPlay = humanPlayer.Hand[cardIndex];
            
            // Act
            var request = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = cardIndex
            };
            
            var result = _controller.PlayCard(request);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PlayCardResponseDto>(okResult.Value);
            
            // Verify that the card was added to the played cards array
            var updatedGame = _gameService.GetGame(game.GameId);
            var playedCard = updatedGame!.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.Equal(cardToPlay.Suit, playedCard.Card.Suit);
            Assert.Equal(cardToPlay.Value, playedCard.Card.Value);
        }
          [Fact]
        public void PlayCard_WithAIPlayers_ShouldTriggerAIResponses()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var cardIndex = 0;
              // Before playing any card, PlayedCards should be empty
            Assert.Empty(game.PlayedCards);
            
            // Act
            var request = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = cardIndex
            };
            
            var result = _controller.PlayCard(request);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PlayCardResponseDto>(okResult.Value);
              // Verify that AI players have also played cards (since AutoAiPlay is enabled)
            var updatedGame = _gameService.GetGame(game.GameId);
              // With AutoAiPlay enabled, AI should play automatically, so at least one AI player should have a card played
            var aiPlayedCards = updatedGame!.PlayedCards
                .Where(pc => pc.PlayerSeat != humanPlayer.Seat && pc.Card != null)
                .ToList();
                
            Assert.True(aiPlayedCards.Count > 0, "At least one AI player should have played a card");
        }
    }
}
