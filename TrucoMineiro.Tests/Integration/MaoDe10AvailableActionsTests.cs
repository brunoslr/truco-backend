using Xunit;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Services;
using TrucoMineiro.Tests.TestUtilities;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Tests for available actions logic with "Mão de 10" special rules
    /// </summary>
    public class MaoDe10AvailableActionsTests
    {
        private readonly TrucoRulesEngine _trucoRulesEngine;
        private readonly MappingService _mappingService;

        public MaoDe10AvailableActionsTests()
        {
            _trucoRulesEngine = new TrucoRulesEngine();
            _mappingService = new MappingService(_trucoRulesEngine);
        }

        [Fact]
        public void GetAvailableActions_WhenOneTeamAt10AndThatTeamActive_ShouldAllowTruco()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            game.CurrentPlayerIndex = 0; // Team 1 player (seat 0)
            
            var player = game.Players[0]; // Team 1 player
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert
            Assert.Contains(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.Fold, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenOneTeamAt10AndOtherTeamActive_ShouldNotAllowTruco()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            game.CurrentPlayerIndex = 1; // Team 2 player (seat 1)
            
            var player = game.Players[1]; // Team 2 player
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 1);
            
            // Assert
            Assert.Contains(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.Fold, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenBothTeamsAt10_ShouldNotAllowTruco()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            game.CurrentPlayerIndex = 0; // Any player
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert
            Assert.Contains(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.Fold, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenRespondingToTrucoAndOneTeamAt10_ShouldAllowRaiseOnlyForTeamAt10()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            game.TrucoCallState = TrucoCallState.Truco;
            game.LastTrucoCallerTeam = 2; // Team 2 called truco
            game.CurrentPlayerIndex = 0; // Team 1 player responding
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert - Team 1 is at 10, so they can raise
            Assert.Contains(TrucoConstants.PlayerActions.AcceptTruco, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.SurrenderTruco, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenRespondingToTrucoAndOtherTeamAt10_ShouldNotAllowRaise()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 8;
            game.TrucoCallState = TrucoCallState.Truco;
            game.LastTrucoCallerTeam = 1; // Team 1 called truco
            game.CurrentPlayerIndex = 1; // Team 2 player responding
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 1);
            
            // Assert - Team 2 is not at 10, so they cannot raise
            Assert.Contains(TrucoConstants.PlayerActions.AcceptTruco, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.SurrenderTruco, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenRespondingToTrucoAndBothTeamsAt10_ShouldNotAllowRaise()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 10;
            game.Team2Score = 10;
            game.TrucoCallState = TrucoCallState.Truco;
            game.LastTrucoCallerTeam = 1; // Team 1 called truco
            game.CurrentPlayerIndex = 1; // Team 2 player responding
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 1);
            
            // Assert - Both teams at 10, no raises allowed
            Assert.Contains(TrucoConstants.PlayerActions.AcceptTruco, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.SurrenderTruco, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenNormalGame_ShouldAllowAllActions()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 8;
            game.Team2Score = 9;
            game.CurrentPlayerIndex = 0;
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert
            Assert.Contains(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.Fold, gameDto.AvailableActions);
        }        [Fact]
        public void GetAvailableActions_WhenMaxStakes_ShouldNotAllowMoreTruco()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 8;
            game.Team2Score = 9;
            game.Stakes = TrucoConstants.Stakes.Maximum; // 12 points (Doze accepted)
            game.TrucoCallState = TrucoCallState.None; // No pending calls - Doze was accepted
            game.CurrentPlayerIndex = 0;
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert
            Assert.Contains(TrucoConstants.PlayerActions.PlayCard, gameDto.AvailableActions);
            Assert.DoesNotContain(TrucoConstants.PlayerActions.CallTrucoOrRaise, gameDto.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.Fold, gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenGameNotActive_ShouldReturnEmptyActions()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.GameStatus = "completed";
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert
            Assert.Empty(gameDto.AvailableActions);
        }

        [Fact]
        public void GetAvailableActions_WhenTeamCalledTruco_ShouldNotGetActionsUntilResponse()
        {
            // Arrange
            var game = TestGameFactory.CreateTestGame();
            game.Team1Score = 8;
            game.Team2Score = 9;
            game.TrucoCallState = TrucoCallState.Truco;
            game.LastTrucoCallerTeam = 1; // Team 1 called truco
            game.CurrentPlayerIndex = 0; // Team 1 player (who called)
            
            // Act
            var gameDto = _mappingService.MapGameStateToDto(game, 0);
            
            // Assert - Team that called truco gets no actions until response
            Assert.Empty(gameDto.AvailableActions);
        }
    }
}
