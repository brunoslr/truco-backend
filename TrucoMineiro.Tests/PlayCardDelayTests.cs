using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrucoMineiro.API.Controllers;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.Services;
using Moq;
using Xunit;

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
        {
            // Set up test configuration with custom delay settings
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
                {"GameSettings:AIPlayDelayMs", "100"},  // Use a small delay for tests
                {"GameSettings:NewHandDelayMs", "200"}  // Use a small delay for tests
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Create a dictionary to store created games (simulating repository storage)
            var gameStorage = new Dictionary<string, GameState>();

            // Create mock services
            var mockGameStateManager = new Mock<IGameStateManager>();
            var mockGameRepository = new Mock<IGameRepository>();
            var mockHandResolutionService = new Mock<IHandResolutionService>();
            var mockTrucoRulesEngine = new Mock<ITrucoRulesEngine>();
            var mockAIPlayerService = new Mock<IAIPlayerService>();
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();

            // Configure mock GameStateManager to return a valid GameState and store it
            mockGameStateManager.Setup(x => x.CreateGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string playerName) => {
                    var gameState = new GameState();
                    gameState.InitializeGame(playerName);
                    gameStorage[gameState.GameId] = gameState;
                    return gameState;
                });
            
            // Configure mock GameRepository to return games from storage
            mockGameRepository.Setup(x => x.GetGameAsync(It.IsAny<string>()))
                .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);

            mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
                .ReturnsAsync((GameState gameState) => {
                    gameStorage[gameState.GameId] = gameState;
                    return true;
                });

            _gameService = new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockHandResolutionService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                configuration);
            _controller = new TrucoGameController(_gameService);
        }

        [Fact]
        public void PlayCard_ShouldUpdatePlayedCardsArray()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var cardIndex = 0;
            
            // Act
            var request = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = cardIndex
            };
            
            var result = _controller.PlayCardEnhanced(request);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PlayCardResponseDto>(okResult.Value);
            
            // Verify that the card was added to the played cards array
            var updatedGame = _gameService.GetGame(game.GameId);
            var playedCard = updatedGame.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.Equal(humanPlayer.Hand[cardIndex].Suit, playedCard.Card.Suit);
            Assert.Equal(humanPlayer.Hand[cardIndex].Value, playedCard.Card.Value);
        }
        
        [Fact]
        public void PlayCard_WithAIPlayers_ShouldTriggerAIResponses()
        {
            // Arrange
            var game = _gameService.CreateGame("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var cardIndex = 0;
            
            // Before playing any card, all AI PlayedCards should be empty
            foreach (var aiPlayer in game.Players.Where(p => p.Seat != 0))
            {
                var aiPlayedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == aiPlayer.Seat);
                Assert.NotNull(aiPlayedCard);
                Assert.Null(aiPlayedCard.Card);
            }
            
            // Act
            var request = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = cardIndex
            };
            
            var result = _controller.PlayCardEnhanced(request);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PlayCardResponseDto>(okResult.Value);
            
            // Verify that AI players have also played cards (since DevMode is enabled)
            var updatedGame = _gameService.GetGame(game.GameId);
            
            // In DevMode, AI should play automatically, so at least one AI player should have a card played
            var aiPlayedCards = updatedGame.PlayedCards
                .Where(pc => pc.PlayerSeat != humanPlayer.Seat && pc.Card != null)
                .ToList();
                
            Assert.True(aiPlayedCards.Count > 0, "At least one AI player should have played a card");
        }
    }
}
