using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using TrucoMineiro.API.Controllers;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Services;
using Moq;
using Xunit;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.StateMachine;
using TrucoMineiro.API.Domain.StateMachine.Commands;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Tests to validate that card play correctly updates player hands and played cards array
    /// </summary>
    public class CardPlayValidationTests
    {
        private readonly GameService _gameService;
        private readonly TrucoGameController _controller;

        public CardPlayValidationTests()
        {
            // Set up test configuration with DevMode disabled for more controlled testing
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
                {"GameSettings:AIPlayDelayMs", "0"},  // No delay for tests
                {"GameSettings:NewHandDelayMs", "0"}  // No delay for tests
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
            var mockScoreCalculationService = new Mock<IScoreCalculationService>();
            var mockEventPublisher = new Mock<IEventPublisher>();// Configure mock GameStateManager to return a valid GameState and store it
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
                .ReturnsAsync((string gameId) => gameStorage.ContainsKey(gameId) ? gameStorage[gameId] : null);

            mockGameRepository.Setup(x => x.SaveGameAsync(It.IsAny<GameState>()))
                .ReturnsAsync((GameState gameState) => {
                    gameStorage[gameState.GameId] = gameState;
                    return true;
                });            // Configure mock GameFlowService to simulate real card playing behavior
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
                });

            // NOTE: ProcessAITurnsAsync is obsolete - AI processing is now event-driven
            // No need to mock this obsolete method as tests should use real event handlers

            mockGameFlowService.Setup(x => x.ProcessHandCompletionAsync(It.IsAny<GameState>(), It.IsAny<int>()))
                .Returns(Task.CompletedTask);            _gameService = new GameService(
                mockGameStateManager.Object,
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockTrucoRulesEngine.Object,
                mockAIPlayerService.Object,
                mockScoreCalculationService.Object,
                mockEventPublisher.Object,
                configuration);

            // Create real GameStateMachine with required dependencies
            var mockHandResolutionService = new Mock<IHandResolutionService>();
            mockHandResolutionService.Setup(x => x.GetCardStrength(It.IsAny<Card>())).Returns(5);
            mockHandResolutionService.Setup(x => x.DetermineRoundWinner(It.IsAny<List<PlayedCard>>(), It.IsAny<List<Player>>()))
                .Returns((List<PlayedCard> playedCards, List<Player> players) => players.FirstOrDefault());
            mockHandResolutionService.Setup(x => x.IsRoundDraw(It.IsAny<List<PlayedCard>>(), It.IsAny<List<Player>>())).Returns(false);
            mockHandResolutionService.Setup(x => x.HandleDrawResolution(It.IsAny<GameState>(), It.IsAny<int>())).Returns((string?)null);
            mockHandResolutionService.Setup(x => x.IsHandComplete(It.IsAny<GameState>())).Returns(false);
            mockHandResolutionService.Setup(x => x.GetHandWinner(It.IsAny<GameState>())).Returns((string?)null);
            
            var mockLogger = new Mock<ILogger<GameStateMachine>>();
            
            var gameStateMachine = new GameStateMachine(
                mockGameRepository.Object,
                mockGameFlowService.Object,
                mockEventPublisher.Object,
                mockAIPlayerService.Object,
                mockHandResolutionService.Object,
                mockLogger.Object);            _controller = new TrucoGameController(_gameService, gameStateMachine);
        }        /// <summary>
        /// Helper method to create and start a game, making it active for card play
        /// </summary>
        private async Task<GameState> CreateAndStartGameAsync(string playerName)
        {            // Start the game using the controller (this creates and starts the game in one step)
            var startRequest = new StartGameRequest { PlayerName = playerName };
            var startResult = await _controller.StartGame(startRequest);
            
            // Verify the game started successfully
            if (startResult.Result is not OkObjectResult okResult)
            {
                // Get the error message for debugging
                string errorMessage = "Unknown error";
                if (startResult.Result is BadRequestObjectResult badRequestResult)
                {
                    errorMessage = badRequestResult.Value?.ToString() ?? "Bad request with no message";
                }
                throw new Exception($"Failed to start game for test: {errorMessage}");
            }
            
            // Extract the game ID from the response
            var startResponse = okResult.Value as StartGameResponse;
            if (startResponse == null)
            {
                throw new Exception("Failed to get start game response");
            }
            
            // Return the game state
            var game = _gameService.GetGame(startResponse.GameId);
            if (game == null)
            {
                throw new Exception("Failed to retrieve created game");
            }
            
            return game;
        }[Fact]
        public async Task PlayCard_ShouldRemoveCardFromPlayerHandAndAddToPlayedCards()
        {
            // Arrange
            var game = await CreateAndStartGameAsync("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var cardIndex = 0;
            
            // Store the card that will be played for later verification
            var cardToPlay = humanPlayer.Hand[cardIndex];
            var originalHandSize = humanPlayer.Hand.Count;
            
            // Act
            var request = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = cardIndex
            };

            var result = await _controller.PlayCard(request);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<PlayCardResponseDto>(okResult.Value);
            Assert.True(response.Success);

            // Get the updated game state
            var updatedGame = _gameService.GetGame(game.GameId);
            var updatedPlayer = updatedGame!.Players.First(p => p.Seat == 0);
            
            // Verify that the card was removed from the player's hand
            Assert.Equal(originalHandSize - 1, updatedPlayer.Hand.Count);
            Assert.DoesNotContain(cardToPlay, updatedPlayer.Hand);
            
            // Verify that the card was added to the played cards array
            var playedCard = updatedGame.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.Equal(cardToPlay.Suit, playedCard.Card.Suit);
            Assert.Equal(cardToPlay.Value, playedCard.Card.Value);
        }        [Fact]
        public async Task PlayCard_WithInvalidCardIndex_ShouldNotModifyGameState()
        {
            // Arrange
            var game = await CreateAndStartGameAsync("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            var invalidCardIndex = 999; // Invalid index
            
            var originalHandSize = humanPlayer.Hand.Count;
            var originalPlayedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            
            // Act
            var request = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = invalidCardIndex
            };

            var result = await _controller.PlayCard(request);
            
            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var response = Assert.IsType<PlayCardResponseDto>(badRequestResult.Value);
            Assert.False(response.Success);

            // Get the updated game state
            var updatedGame = _gameService.GetGame(game.GameId);
            var updatedPlayer = updatedGame!.Players.First(p => p.Seat == 0);
            
            // Verify that the player's hand was not modified
            Assert.Equal(originalHandSize, updatedPlayer.Hand.Count);

            // Verify that the played cards array was not modified (should remain empty)
            var playedCard = updatedGame.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.Null(playedCard); // Should be null since no card was played
        }        [Fact]
        public async Task PlayCard_MultipleCards_ShouldMaintainCorrectHandSizes()
        {
            // Arrange
            var game = await CreateAndStartGameAsync("TestPlayer");
            var humanPlayer = game.Players.First(p => p.Seat == 0);
            
            var originalHandSize = humanPlayer.Hand.Count;
            var cardsToPlay = new List<Card>
            {
                humanPlayer.Hand[0],
                humanPlayer.Hand[1]
            };
            
            // Act - Play first card
            var request1 = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = 0
            };
            
            var result1 = await _controller.PlayCard(request1);

            // Get updated state after first card
            var gameAfterFirst = _gameService.GetGame(game.GameId);
            var playerAfterFirst = gameAfterFirst!.Players.First(p => p.Seat == 0);
            
            // Make the human player active again for the second card (simulate game flow)
            playerAfterFirst.IsActive = true;
            
            // Act - Play second card (now at index 0 since first card was removed)
            var request2 = new PlayCardRequestDto
            {
                GameId = game.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = 0
            };
            
            var result2 = await _controller.PlayCard(request2);
            
            // Assert
            var okResult1 = Assert.IsType<OkObjectResult>(result1.Result);
            var response1 = Assert.IsType<PlayCardResponseDto>(okResult1.Value);
            Assert.True(response1.Success);
            
            var okResult2 = Assert.IsType<OkObjectResult>(result2.Result);
            var response2 = Assert.IsType<PlayCardResponseDto>(okResult2.Value);
            Assert.True(response2.Success);

            // Get final game state
            var finalGame = _gameService.GetGame(game.GameId);
            var finalPlayer = finalGame!.Players.First(p => p.Seat == 0);
            
            // Verify that the player's hand size decreased correctly
            Assert.Equal(originalHandSize - 2, finalPlayer.Hand.Count);
            
            // Verify that neither of the played cards are still in the hand
            Assert.DoesNotContain(cardsToPlay[0], finalPlayer.Hand);
            Assert.DoesNotContain(cardsToPlay[1], finalPlayer.Hand);

            // Verify that the last played card is in the played cards array
            var playedCard = finalGame.PlayedCards.LastOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.Equal(cardsToPlay[1].Suit, playedCard.Card.Suit);
            Assert.Equal(cardsToPlay[1].Value, playedCard.Card.Value);
        }
    }
}
