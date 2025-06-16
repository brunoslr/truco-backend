using Xunit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Moq;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Constants;
using TrucoMineiro.Tests.TestUtilities;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Tests for "Mão de 10" special rules implementation
    /// </summary>
    public class MaoDe10SpecialRulesTests
    {
        private readonly Mock<IGameRepository> _mockGameRepository;
        private readonly Mock<ILogger<HandStartedEventHandler>> _mockLogger;
        private readonly HandStartedEventHandler _handler;
        private readonly TrucoRulesEngine _trucoRulesEngine;

        public MaoDe10SpecialRulesTests()
        {
            _mockGameRepository = new Mock<IGameRepository>();
            _mockLogger = new Mock<ILogger<HandStartedEventHandler>>();
            _trucoRulesEngine = new TrucoRulesEngine();
            
            _handler = new HandStartedEventHandler(
                _mockGameRepository.Object,
                _trucoRulesEngine,
                _mockLogger.Object);
        }

        [Fact]
        public async Task HandleAsync_WhenOneTeamAt10_ShouldApplyMaoDe10Rules()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            
            var handStartedEvent = new HandStartedEvent(gameId, 2, 1, 2, game);
            
            _mockGameRepository.Setup(r => r.GetGameAsync(gameId.ToString()))
                              .ReturnsAsync(game);
            
            // Act
            await _handler.HandleAsync(handStartedEvent);
            
            // Assert
            Assert.True(_trucoRulesEngine.IsOneTeamAt10(game));
            Assert.False(_trucoRulesEngine.AreBothTeamsAt10(game));
            Assert.Equal(TrucoConstants.Stakes.TrucoCall, game.Stakes); // 4 points for one team at 10
            Assert.Equal(TrucoCallState.Truco, game.TrucoCallState);
            Assert.Equal(1, game.LastTrucoCallerTeam); // Team 1 is at 10
            Assert.Null(game.CanRaiseTeam);
            Assert.False(game.IsBothTeamsAt10);
            
            _mockGameRepository.Verify(r => r.SaveGameAsync(game), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenBothTeamsAt10_ShouldApplyBothTeamsMaoDe10Rules()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            
            var handStartedEvent = new HandStartedEvent(gameId, 2, 1, 2, game);
            
            _mockGameRepository.Setup(r => r.GetGameAsync(gameId.ToString()))
                              .ReturnsAsync(game);
            
            // Act
            await _handler.HandleAsync(handStartedEvent);
            
            // Assert
            Assert.False(_trucoRulesEngine.IsOneTeamAt10(game));
            Assert.True(_trucoRulesEngine.AreBothTeamsAt10(game));
            Assert.Equal(TrucoConstants.Stakes.Initial, game.Stakes); // Normal 2-point hand
            Assert.Equal(TrucoCallState.None, game.TrucoCallState);
            Assert.Equal(-1, game.LastTrucoCallerTeam);
            Assert.Null(game.CanRaiseTeam);
            Assert.True(game.IsBothTeamsAt10);
            
            _mockGameRepository.Verify(r => r.SaveGameAsync(game), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_WhenNoTeamAt10_ShouldNotApplyMaoDe10Rules()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 8;
            game.Team2Score = 9;
            
            var handStartedEvent = new HandStartedEvent(gameId, 2, 1, 2, game);
            
            _mockGameRepository.Setup(r => r.GetGameAsync(gameId.ToString()))
                              .ReturnsAsync(game);
            
            // Act
            await _handler.HandleAsync(handStartedEvent);
            
            // Assert
            Assert.False(_trucoRulesEngine.IsOneTeamAt10(game));
            Assert.False(_trucoRulesEngine.AreBothTeamsAt10(game));
            Assert.False(_trucoRulesEngine.IsMaoDe10Active(game));
            
            // Should have normal reset truco state
            Assert.Equal(TrucoConstants.Stakes.Initial, game.Stakes);
            Assert.Equal(TrucoCallState.None, game.TrucoCallState);
            Assert.Equal(-1, game.LastTrucoCallerTeam);
            Assert.Null(game.CanRaiseTeam);
            Assert.False(game.IsBothTeamsAt10);
            
            _mockGameRepository.Verify(r => r.SaveGameAsync(game), Times.Once);
        }

        [Fact]
        public void IsMaoDe10Active_WhenOneTeamAt10_ShouldReturnTrue()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            
            // Act & Assert
            Assert.True(_trucoRulesEngine.IsMaoDe10Active(game));
        }

        [Fact]
        public void IsMaoDe10Active_WhenBothTeamsAt10_ShouldReturnTrue()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            
            // Act & Assert
            Assert.True(_trucoRulesEngine.IsMaoDe10Active(game));
        }

        [Fact]
        public void IsMaoDe10Active_WhenNoTeamAt10_ShouldReturnFalse()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 9;
            game.Team2Score = 8;
            
            // Act & Assert
            Assert.False(_trucoRulesEngine.IsMaoDe10Active(game));
        }        [Fact]
        public void IsOneTeamAt10_WhenTeam1At10_ShouldReturnTrue()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            
            // Act & Assert
            Assert.True(_trucoRulesEngine.IsOneTeamAt10(game));
            Assert.Equal(Team.PlayerTeam, _trucoRulesEngine.GetTeamAt10(game));
        }

        [Fact]
        public void IsOneTeamAt10_WhenTeam2At10_ShouldReturnTrue()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 8;
            game.Team2Score = 10;
              // Act & Assert
            Assert.True(_trucoRulesEngine.IsOneTeamAt10(game));
            Assert.Equal(Team.OpponentTeam, _trucoRulesEngine.GetTeamAt10(game));
        }

        [Fact]
        public void IsOneTeamAt10_WhenBothTeamsAt10_ShouldReturnFalse()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            
            // Act & Assert
            Assert.False(_trucoRulesEngine.IsOneTeamAt10(game));
        }

        [Fact]
        public void AreBothTeamsAt10_WhenBothAt10_ShouldReturnTrue()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            
            // Act & Assert
            Assert.True(_trucoRulesEngine.AreBothTeamsAt10(game));
        }

        [Fact]
        public void AreBothTeamsAt10_WhenOnlyOneAt10_ShouldReturnFalse()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 9;
            
            // Act & Assert
            Assert.False(_trucoRulesEngine.AreBothTeamsAt10(game));
        }

        [Fact]
        public void GetTeamAt10_WhenTeam1At10_ShouldReturnTeam1()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
              // Act & Assert
            Assert.Equal(Team.PlayerTeam, _trucoRulesEngine.GetTeamAt10(game));
        }

        [Fact]
        public void GetTeamAt10_WhenTeam2At10_ShouldReturnTeam2()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 8;
            game.Team2Score = 10;
              // Act & Assert
            Assert.Equal(Team.OpponentTeam, _trucoRulesEngine.GetTeamAt10(game));
        }

        [Fact]
        public void GetTeamAt10_WhenBothTeamsAt10_ShouldReturnNull()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            
            // Act & Assert
            Assert.Null(_trucoRulesEngine.GetTeamAt10(game));
        }

        [Fact]
        public void GetTeamAt10_WhenNoTeamAt10_ShouldReturnNull()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 9;
            game.Team2Score = 8;
            
            // Act & Assert
            Assert.Null(_trucoRulesEngine.GetTeamAt10(game));
        }
    }
}
