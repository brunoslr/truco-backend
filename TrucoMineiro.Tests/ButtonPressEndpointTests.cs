using TrucoMineiro.API.Constants;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Comprehensive endpoint tests for Button Press functionality (Truco/Raise/Accept/Surrender)
    /// Tests the complete API integration for the new stakes progression system
    /// Uses real HTTP requests and event-driven architecture
    /// 
    /// TESTS COVERAGE:    /// - API endpoint functionality for all button actions
    /// - Request/response validation and error handling
    /// - Game state updates after button press actions
    /// - Available actions calculation based on game state
    /// - Team alternation rules and truco progression
    /// - Last hand special rules validation
    /// </summary>
    public class ButtonPressEndpointTests : EndpointTestBase
    {
        [Fact]
        public async Task PressButton_CallTrucoOrRaise_ShouldStartTrucoProgression()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "CallTrucoOrRaise");            // Act
            var gameState = await PressButtonAsync(request);

            // Assert
            Assert.Equal("Truco", gameState.TrucoCallState);
            Assert.Equal(4, gameState.CurrentStakes); // Truco raises stakes to 4
            Assert.Equal((int)Team.PlayerTeam, gameState.LastTrucoCallerTeam); // Human player team called
            
            // The calling player should NOT have play-card available - they must wait for opponent response
            // (AvailableActions are calculated for the requesting player, which is the human who called truco)
            Assert.DoesNotContain(TrucoConstants.PlayerActions.PlayCard, gameState.AvailableActions);
            // In fact, the calling player should have no available actions until truco is resolved
            Assert.Empty(gameState.AvailableActions);
        }

        [Fact]
        public async Task PressButton_AcceptTruco_ShouldConfirmStakes()
        {
            // Arrange - Set up a game where opponent team called truco
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            
            // First, simulate an AI calling truco (this would normally happen via AI service)
            // For testing, we'll directly set up a state where truco was called by AI
            // and human needs to respond
            
            // Get an AI player's seat for the truco call
            var aiPlayer = gameResponse.Players.First(p => p.Seat == 1); // AI player
            var trucoCallRequest = CreateButtonPressRequest(gameResponse.GameId, aiPlayer.Seat, "CallTrucoOrRaise");
            await PressButtonAsync(trucoCallRequest);
            
            // Now human accepts the truco
            var acceptRequest = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "AcceptTruco");

            // Act
            var gameState = await PressButtonAsync(acceptRequest);

            // Assert
            Assert.Equal("Truco", gameState.TrucoCallState);
            Assert.Equal(4, gameState.Stakes); // Stakes confirmed at 4
            Assert.Equal((int)Team.OpponentTeam, gameState.LastTrucoCallerTeam); // AI team called
            Assert.Equal((int)Team.PlayerTeam, gameState.CanRaiseTeam); // Human team can now raise
        }

        [Fact]
        public async Task PressButton_SurrenderTruco_ShouldAwardPointsToOpponent()
        {
            // Arrange - Set up truco call by AI team
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var aiPlayer = gameResponse.Players.First(p => p.Seat == 1);
            
            // AI calls truco
            var trucoCallRequest = CreateButtonPressRequest(gameResponse.GameId, aiPlayer.Seat, "CallTrucoOrRaise");
            await PressButtonAsync(trucoCallRequest);
            
            // Human surrenders
            var surrenderRequest = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "SurrenderTruco");

            // Act
            var gameState = await PressButtonAsync(surrenderRequest);

            // Assert
            // Points should be awarded to the opponent team
            Assert.True(gameState.TeamScores[Team.OpponentTeam] >= 4); // AI team got points
            // Game should continue or end depending on score
        }        [Fact]
        public async Task PressButton_RaiseProgression_ShouldFollowTrucoSeisNoveDozeFlow()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var aiPlayer = gameResponse.Players.First(p => p.Seat == 1);

            // Act & Assert - Test the progression: None → Truco → Seis → Nove → Doze
            
            // 1. Human calls Truco (None → Truco)
            var trucoRequest = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "CallTrucoOrRaise");
            var gameState1 = await PressButtonAsync(trucoRequest);
            Assert.Equal("Truco", gameState1.TrucoCallState);
            Assert.Equal(4, gameState1.CurrentStakes);

            // 2. AI raises to Seis (Truco → Seis)
            var seisRequest = CreateButtonPressRequest(gameResponse.GameId, aiPlayer.Seat, "CallTrucoOrRaise");
            var gameState2 = await PressButtonAsync(seisRequest);
            Assert.Equal("Seis", gameState2.TrucoCallState);
            Assert.Equal(8, gameState2.CurrentStakes);

            // 3. Human raises to Nove (Seis → Nove)
            var noveRequest = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "CallTrucoOrRaise");
            var gameState3 = await PressButtonAsync(noveRequest);
            Assert.Equal("Nove", gameState3.TrucoCallState);
            Assert.Equal(10, gameState3.CurrentStakes);

            // 4. AI raises to Doze (Nove → Doze)
            var dozeRequest = CreateButtonPressRequest(gameResponse.GameId, aiPlayer.Seat, "CallTrucoOrRaise");
            var gameState4 = await PressButtonAsync(dozeRequest);
            Assert.Equal("Doze", gameState4.TrucoCallState);
            Assert.Equal(12, gameState4.CurrentStakes);

            // 5. Cannot raise beyond Doze
            // The available actions should not include CallTrucoOrRaise for human at this point
        }

        [Fact]
        public async Task PressButton_InvalidAction_ShouldReturnBadRequest()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "InvalidAction");

            // Act & Assert
            var exception = await Assert.ThrowsAsync<HttpRequestException>(
                () => PressButtonAsync(request));
            
            // Should contain error about invalid action
            Assert.Contains("Invalid action", exception.Message);
        }

        [Fact]
        public async Task PressButton_SameTeamConsecutiveCalls_ShouldReturnError()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);            // Human calls truco
            var firstRequest = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "CallTrucoOrRaise");
            var gameStateAfterFirst = await PressButtonAsync(firstRequest);            // Verify first call succeeded and check team
            Assert.Equal("Truco", gameStateAfterFirst.TrucoCallState);
            Assert.True(gameStateAfterFirst.LastTrucoCallerTeam != -1, "LastTrucoCallerTeam should be set after first call");// Try to call again with same team (should fail)
            var secondRequest = CreateButtonPressRequest(gameResponse.GameId, humanPlayer.Seat, "CallTrucoOrRaise");

            // Act & Assert
            try
            {
                var secondResponse = await PressButtonAsync(secondRequest);                // If we get here, the call succeeded when it shouldn't have
                Assert.Fail($"Second consecutive call succeeded when it should have failed. TrucoCallState: {secondResponse.TrucoCallState}, LastTrucoCallerTeam: {secondResponse.LastTrucoCallerTeam}");
            }            catch (HttpRequestException ex)
            {
                // The backend returns a generic error when CanCallTruco returns false
                // The specific check is in the rules engine, but the error message is generic
                Assert.Contains("Cannot call truco in current game state", ex.Message);
            }
        }

        [Fact]
        public async Task PressButton_AvailableActions_ShouldReflectGameState()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);

            // Act - Get initial state (no truco call)
            var initialState = await GetGameStateAsync(gameResponse.GameId, humanPlayer.Seat);

            // Assert - Should have normal actions available
            Assert.Contains(TrucoConstants.PlayerActions.PlayCard, initialState.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.CallTrucoOrRaise, initialState.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.Fold, initialState.AvailableActions);

            // Act - AI calls truco
            var aiPlayer = gameResponse.Players.First(p => p.Seat == 1);
            var trucoRequest = CreateButtonPressRequest(gameResponse.GameId, aiPlayer.Seat, "CallTrucoOrRaise");
            await PressButtonAsync(trucoRequest);

            // Get state from human player perspective
            var trucoState = await GetGameStateAsync(gameResponse.GameId, humanPlayer.Seat);

            // Assert - Should now have truco response actions
            Assert.Contains(TrucoConstants.PlayerActions.AcceptTruco, trucoState.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.SurrenderTruco, trucoState.AvailableActions);
            Assert.Contains(TrucoConstants.PlayerActions.CallTrucoOrRaise, trucoState.AvailableActions); // Can counter-raise
        }
    }
}
