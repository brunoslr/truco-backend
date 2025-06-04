using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.Services;
using Xunit;

namespace TrucoMineiro.Tests
{    public class StartGameTests
    {
        private readonly IConfiguration _configuration;

        public StartGameTests()
        {
            // Set up test configuration
            var inMemorySettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "false"},
            };

            _configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        [Fact]
        public void CreateGameWithCustomName_ShouldInitializeGameState()
        {
            // Arrange
            var gameService = new GameService(_configuration);
            string playerName = "TestPlayer";

            // Act
            var game = gameService.CreateGame(playerName);

            // Assert
            Assert.NotNull(game);
            Assert.Equal(4, game.Players.Count);
            Assert.Equal(2, game.Stakes); // Stakes should be 2 as specified in requirements
            Assert.False(game.IsTrucoCalled);
            Assert.True(game.IsRaiseEnabled);
            Assert.Equal(1, game.CurrentHand);
            Assert.Equal(2, game.TeamScores.Count);
            Assert.Equal(0, game.TeamScores["Player's Team"]);
            Assert.Equal(0, game.TeamScores["Opponent Team"]);
            
            // Each player should have 3 cards
            foreach (var player in game.Players)
            {
                Assert.Equal(3, player.Hand.Count);
            }
            
            // Player at seat 0 should have the custom name
            Assert.Equal(playerName, game.Players.First(p => p.Seat == 0).Name);
            
            // Other players should have default names
            Assert.Equal("AI 1", game.Players.First(p => p.Seat == 1).Name);
            Assert.Equal("Partner", game.Players.First(p => p.Seat == 2).Name);
            Assert.Equal("AI 2", game.Players.First(p => p.Seat == 3).Name);
        }

        [Fact]
        public void MapGameStateToStartGameResponse_ShouldMapCorrectly()
        {
            // Arrange
            var gameService = new GameService(_configuration);
            string playerName = "TestPlayer";
            var game = gameService.CreateGame(playerName);

            // Act
            var response = MappingService.MapGameStateToStartGameResponse(game);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(game.GameId, response.GameId);
            Assert.Equal(0, response.PlayerSeat); // Single player is always at seat 0
            Assert.Equal(game.DealerSeat, response.DealerSeat);
            Assert.Equal(2, response.Stakes); // Stakes should be 2
            Assert.Equal(1, response.CurrentHand);
            Assert.Equal(2, response.TeamScores.Count);
            Assert.Equal(0, response.TeamScores["Player's Team"]);
            Assert.Equal(0, response.TeamScores["Opponent Team"]);
            Assert.Empty(response.Actions);
            
            // Teams should be mapped correctly
            Assert.Equal(2, response.Teams.Count);
            var playerTeam = response.Teams.First(t => t.Name == "Player's Team");
            var opponentTeam = response.Teams.First(t => t.Name == "Opponent Team");
            Assert.Equal(2, playerTeam.Seats.Count);
            Assert.Equal(2, opponentTeam.Seats.Count);
            Assert.Contains(0, playerTeam.Seats);
            Assert.Contains(2, playerTeam.Seats);
            Assert.Contains(1, opponentTeam.Seats);
            Assert.Contains(3, opponentTeam.Seats);

            // Players should be mapped correctly
            Assert.Equal(4, response.Players.Count);
            Assert.Equal(playerName, response.Players.First(p => p.Seat == 0).Name);
            Assert.Equal("Player's Team", response.Players.First(p => p.Seat == 0).Team);
            
            // Hand should have 3 cards for player at seat 0            Assert.Equal(3, response.Hand.Count);
        }
        
        [Fact]
        public void MapGameStateToStartGameResponse_WithDevMode_ShouldShowCards()
        {
            // Arrange
            var devModeSettings = new Dictionary<string, string?> {
                {"FeatureFlags:DevMode", "true"},
            };
            var devConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(devModeSettings)
                .Build();

            var gameService = new GameService(devConfig);
            string playerName = "TestPlayer";
            var game = gameService.CreateGame(playerName);

            // Act - Note we pass true for showAllHands param
            var response = MappingService.MapGameStateToStartGameResponse(game, 0, true);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(3, response.Hand.Count); // Player's cards should be visible
        }
    }
}
