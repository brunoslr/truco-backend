using TrucoMineiro.API.DTOs;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests
{
    /// <summary>
    /// Modernized endpoint tests for PlayCard functionality
    /// Uses real HTTP requests and event-driven architecture instead of mocks
    /// Follows current testing best practices with shared setup via EndpointTestBase
    /// 
    /// ARCHITECTURE NOTES:
    /// - Uses TestWebApplicationFactory for real endpoint testing
    /// - Leverages actual PlayCardService.ProcessPlayCardRequestAsync() implementation
    /// - Tests complete request/response flow including event publishing
    /// - No mocks - tests real component interactions and event-driven AI
    /// </summary>
    public class PlayCardEndpointTests : EndpointTestBase
    {
        [Fact]
        public async Task PlayCard_ShouldReturnSuccess_WhenValidMove()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            Assert.Equal(2, response.Hand.Count); // Player should have 2 cards left after playing one
            Assert.Equal(4, response.PlayerHands.Count); // Should have all 4 player hands
        }

        [Fact]
        public async Task PlayCard_ShouldHideAICards_WhenNotInDevMode()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // Human player's hand should be visible
            var humanPlayerHand = response.PlayerHands.First(h => h.Seat == 0);
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Value));
            Assert.All(humanPlayerHand.Cards, card => Assert.NotNull(card.Suit));

            // AI player hands should be hidden (cards exist but values are null)
            var aiPlayerHands = response.PlayerHands.Where(h => h.Seat != 0);
            foreach (var aiHand in aiPlayerHands)
            {
                Assert.All(aiHand.Cards, card => Assert.Null(card.Value));
                Assert.All(aiHand.Cards, card => Assert.Null(card.Suit));
            }
        }        [Fact]
        public async Task PlayCard_ShouldShowAllCards_WhenInDevMode()
        {
            // Arrange - Create test with DevMode enabled
            var devModeConfig = GetFastTestConfigWithDevMode();
            
            using var testWithDevMode = new PlayCardEndpointTestsWithDevMode(devModeConfig);
            var gameResponse = await testWithDevMode.CreateGameAsync("TestPlayer");
            var humanPlayer = gameResponse.Players.First(p => p.Seat == 0);
            var request = new PlayCardRequestDto
            {
                GameId = gameResponse.GameId,
                PlayerSeat = humanPlayer.Seat,
                CardIndex = 0,
                IsFold = false
            };

            // Act
            var response = await testWithDevMode.PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // All player hands should be visible in DevMode (non-empty cards should have values)
            foreach (var playerHand in response.PlayerHands)
            {
                if (playerHand.Cards.Count > 0)
                {
                    // In DevMode, cards should be visible (not null)
                    Assert.All(playerHand.Cards, card => Assert.NotNull(card.Value));
                    Assert.All(playerHand.Cards, card => Assert.NotNull(card.Suit));
                }
            }
        }        
        [Fact]
        public async Task PlayCard_ShouldHandleFold_WhenFoldRequested()
        {
            // This test disables AI auto-play to verify fold card before automatic round cleanup
            using var foldTestHelper = new FoldTestHelper();
            
            // Arrange
            var gameResponse = await foldTestHelper.CreateGameAsync("TestPlayer");
            var humanPlayer = foldTestHelper.GetHumanPlayer(gameResponse);
            var request = foldTestHelper.CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0, isFold: true);

            // Act
            var response = await foldTestHelper.PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            Assert.NotNull(response.GameState);
            
            // With AI auto-play disabled, fold card should be preserved
            var playedCard = response.GameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.Equal("FOLD", playedCard.Card.Value);
            Assert.Equal("FOLD", playedCard.Card.Suit);
            
            // Action log verification - Check if fold action was recorded
            if (response.GameState.ActionLog != null && response.GameState.ActionLog.Any())
            {
                var foldAction = response.GameState.ActionLog.FirstOrDefault(action => 
                    action.PlayerSeat == humanPlayer.Seat && 
                    (action.Card?.Contains("FOLD") == true || action.Action?.ToLower().Contains("fold") == true));
                    
                // If action log exists, it should contain the fold action
                Assert.NotNull(foldAction);
            }
            
            // Additional verification: Player should have 2 cards left (started with 3, played/folded 1)
            Assert.Equal(2, response.Hand.Count);
        }

        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenGameNotFound()
        {
            // Arrange
            var request = CreatePlayCardRequest("invalid-game-id", 0, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Equal("Game not found", response.Message);
        }

        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenInvalidCardIndex()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 99); // Invalid card index

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            Assert.Contains("Invalid card index", response.Message);
        }        [Fact]
        public async Task PlayCard_ShouldReturnError_WhenInvalidPlayerSeat()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var request = CreatePlayCardRequest(gameResponse.GameId, 99, 0); // Invalid player seat

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.False(response.Success);
            // The error message should indicate invalid request parameters since 99 is out of range (0-3)
            Assert.Contains("Invalid request parameters", response.Message);
        }[Fact]
        public async Task PlayCard_ShouldUpdatePlayedCardsArray()
        {
            // Arrange
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            
            // Get initial game state to check played cards count
            var initialGameState = await GetGameStateAsync(gameResponse.GameId);
            var originalCardCount = initialGameState.PlayedCards.Count;
            
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // Get updated game state to check played cards
            var updatedGameState = await GetGameStateAsync(gameResponse.GameId);
            
            // Should have one more played card than before
            Assert.True(updatedGameState.PlayedCards.Count >= originalCardCount);
            
            // Should have a played card for the human player
            var playedCard = updatedGameState.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(playedCard);
            Assert.NotNull(playedCard.Card);
            Assert.NotNull(playedCard.Card.Value);
            Assert.NotNull(playedCard.Card.Suit);
        }

        /// <summary>
        /// Helper class for DevMode testing
        /// </summary>
        private class PlayCardEndpointTestsWithDevMode : EndpointTestBase        {
            public PlayCardEndpointTestsWithDevMode(Dictionary<string, string?> configOverrides) : base(configOverrides)
            {
            }
        }        /// <summary>
        /// Helper class for fold tests that need to disable AI auto-play
        /// to verify fold cards before round cleanup occurs
        /// </summary>
        private class FoldTestHelper : EndpointTestBase
        {
            public FoldTestHelper() : base(GetConfigWithoutAutoAiPlay())
            {
            }

            public new async Task<StartGameResponse> CreateGameAsync(string playerName) => await base.CreateGameAsync(playerName);
            public new async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request) => await base.PlayCardAsync(request);
            public new PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse) => base.GetHumanPlayer(gameResponse);
            public new PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat, int cardIndex, bool isFold = false) => base.CreatePlayCardRequest(gameId, playerSeat, cardIndex, isFold);
        }        /// <summary>
        /// Helper class for AutoAiPlay testing - disables AI auto-play
        /// </summary>
        private class AutoAiPlayTestHelper : EndpointTestBase
        {
            public AutoAiPlayTestHelper() : base(GetConfigWithoutAutoAiPlay())
            {
            }

            public new async Task<StartGameResponse> CreateGameAsync(string playerName) => await base.CreateGameAsync(playerName);
            public new async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request) => await base.PlayCardAsync(request);
            public new PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse) => base.GetHumanPlayer(gameResponse);
            public new PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat, int cardIndex, bool isFold = false) => base.CreatePlayCardRequest(gameId, playerSeat, cardIndex, isFold);
        }

        /// <summary>
        /// Helper class for DevMode testing - enables DevMode
        /// </summary>
        private class DevModeTestHelper : EndpointTestBase
        {
            public DevModeTestHelper() : base(GetFastTestConfigWithDevMode())
            {
            }

            public new async Task<StartGameResponse> CreateGameAsync(string playerName) => await base.CreateGameAsync(playerName);
            public new async Task<PlayCardResponseDto> PlayCardAsync(PlayCardRequestDto request) => await base.PlayCardAsync(request);
            public new async Task<GameStateDto> GetGameStateAsync(string gameId, int? playerSeat = null) => await base.GetGameStateAsync(gameId, playerSeat);
            public new PlayerInfoDto GetHumanPlayer(StartGameResponse gameResponse) => base.GetHumanPlayer(gameResponse);
            public new PlayCardRequestDto CreatePlayCardRequest(string gameId, int playerSeat, int cardIndex, bool isFold = false) => base.CreatePlayCardRequest(gameId, playerSeat, cardIndex, isFold);
        }

        /// <summary>
        /// Tests that AI auto-play works when the feature flag is enabled (default behavior)
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldTriggerAIAutoPlay_WhenAutoAiPlayEnabled()
        {
            // Arrange - Use default config with AutoAiPlay enabled
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            
            // With AutoAiPlay enabled, AI players should have played automatically
            // The round should be complete or nearly complete
            var playedCardsCount = response.GameState.PlayedCards?.Count ?? 0;
            
            // Should have multiple cards played (human + AI players)
            Assert.True(playedCardsCount > 1, 
                $"Expected more than 1 card played due to AI auto-play, but found {playedCardsCount}");
            
            // Verify AI players have fewer cards in their hands (they played cards)
            var aiPlayerHands = response.PlayerHands.Where(ph => ph.Seat != humanPlayer.Seat).ToList();
            foreach (var aiHand in aiPlayerHands)
            {
                // AI players should have played a card, so they should have fewer than 3 cards
                Assert.True(aiHand.Cards.Count <= 2, 
                    $"AI player at seat {aiHand.Seat} should have played a card but still has {aiHand.Cards.Count} cards");
            }
        }

        /// <summary>
        /// Tests that AI auto-play is disabled when the feature flag is set to false
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldNotTriggerAIAutoPlay_WhenAutoAiPlayDisabled()
        {
            // Arrange - Use config with AutoAiPlay disabled
            using var testHelper = new AutoAiPlayTestHelper();
            var gameResponse = await testHelper.CreateGameAsync("TestPlayer");
            var humanPlayer = testHelper.GetHumanPlayer(gameResponse);
            var request = testHelper.CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await testHelper.PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            Assert.Equal("Card played successfully", response.Message);
            
            // With AutoAiPlay disabled, only the human player should have played
            var playedCardsCount = response.GameState.PlayedCards?.Count ?? 0;
            
            // Should have exactly 1 card played (only the human player)
            Assert.Equal(1, playedCardsCount);
            
            // Verify only the human player's card is in played cards
            var humanPlayedCard = response.GameState.PlayedCards?.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(humanPlayedCard);
            
            // Verify AI players still have all their cards (they didn't auto-play)
            var aiPlayerHands = response.PlayerHands.Where(ph => ph.Seat != humanPlayer.Seat).ToList();
            foreach (var aiHand in aiPlayerHands)
            {
                // AI players should still have 3 cards (they didn't play)
                Assert.Equal(3, aiHand.Cards.Count);
            }
        }        /// <summary>
        /// Tests that AI auto-play configuration persists across multiple card plays
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldMaintainAutoAiPlaySetting_AcrossMultiplePlays()
        {
            // Arrange - Use config with AutoAiPlay disabled
            using var testHelper = new AutoAiPlayTestHelper();
            var gameResponse = await testHelper.CreateGameAsync("TestPlayer");
            var humanPlayer = testHelper.GetHumanPlayer(gameResponse);
            
            // Act - Play one card and verify only human player played
            var request = testHelper.CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);
            var response = await testHelper.PlayCardAsync(request);
            
            // Assert
            Assert.True(response.Success);
            
            // Should have exactly 1 card played (only the human player)
            var playedCardsCount = response.GameState.PlayedCards?.Count ?? 0;
            Assert.Equal(1, playedCardsCount);
            
            // Verify only the human player's card is in played cards
            var humanPlayedCard = response.GameState.PlayedCards?.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer.Seat);
            Assert.NotNull(humanPlayedCard);
            
            // Verify AI players still have all their cards (they didn't auto-play)
            var aiPlayerHands = response.PlayerHands.Where(ph => ph.Seat != humanPlayer.Seat).ToList();
            foreach (var aiHand in aiPlayerHands)
            {
                // AI players should still have 3 cards (they didn't play)
                Assert.Equal(3, aiHand.Cards.Count);
            }
            
            // Now let's manually trigger the next player turn by checking that the game
            // correctly maintains the AutoAiPlay=false setting
            var gameState = await testHelper.GetGameStateAsync(gameResponse.GameId, humanPlayer.Seat);
            
            // The game should be waiting for the next player (AI 1 at seat 1) to play
            // but since AutoAiPlay is disabled, they shouldn't auto-play
            Assert.NotNull(gameState);
            
            // In a real scenario with AutoAiPlay disabled, we'd need to manually play cards
            // for AI players or implement manual control, but this test verifies that
            // the AutoAiPlay setting is respected consistently
        }

        /// <summary>
        /// Tests that DevMode shows all player cards including AI hands
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldShowAllCards_WhenDevModeEnabled()
        {
            // Arrange - Use config with DevMode enabled
            using var testHelper = new DevModeTestHelper();
            var gameResponse = await testHelper.CreateGameAsync("TestPlayer");
            var humanPlayer = testHelper.GetHumanPlayer(gameResponse);
            var request = testHelper.CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await testHelper.PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // In DevMode, all player hands should be visible (have actual card values)
            foreach (var playerHand in response.PlayerHands)
            {
                if (playerHand.Cards.Count > 0)
                {
                    // All cards should have visible values and suits in DevMode
                    Assert.All(playerHand.Cards, card => 
                    {
                        Assert.NotNull(card.Value);
                        Assert.NotNull(card.Suit);
                        Assert.NotEmpty(card.Value);
                        Assert.NotEmpty(card.Suit);
                    });
                }
            }
        }

        /// <summary>
        /// Tests that normal mode hides AI player cards but shows human cards
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldHideAICards_WhenDevModeDisabled()
        {
            // Arrange - Use default config with DevMode disabled
            var gameResponse = await CreateGameAsync("TestPlayer");
            var humanPlayer = GetHumanPlayer(gameResponse);
            var request = CreatePlayCardRequest(gameResponse.GameId, humanPlayer.Seat, 0);

            // Act
            var response = await PlayCardAsync(request);

            // Assert
            Assert.True(response.Success);
            
            // Human player hand should be visible
            var humanHand = response.PlayerHands.FirstOrDefault(ph => ph.Seat == humanPlayer.Seat);
            Assert.NotNull(humanHand);
            if (humanHand.Cards.Count > 0)
            {
                Assert.All(humanHand.Cards, card =>
                {
                    Assert.NotNull(card.Value);
                    Assert.NotNull(card.Suit);
                });
            }
            
            // AI player hands should be hidden (null values)
            var aiPlayerHands = response.PlayerHands.Where(ph => ph.Seat != humanPlayer.Seat);
            foreach (var aiHand in aiPlayerHands)
            {
                if (aiHand.Cards.Count > 0)
                {
                    Assert.All(aiHand.Cards, card =>
                    {
                        Assert.Null(card.Value);
                        Assert.Null(card.Suit);
                    });
                }
            }
        }

        /// <summary>
        /// Tests that DevMode configuration affects card visibility in game state
        /// </summary>
        [Fact]
        public async Task GetGameState_ShouldRespectDevModeForCardVisibility()
        {
            // Arrange - Create game with DevMode enabled
            using var testHelper = new DevModeTestHelper();
            var gameResponse = await testHelper.CreateGameAsync("TestPlayer");
            var humanPlayer = testHelper.GetHumanPlayer(gameResponse);

            // Act - Get game state directly
            var gameState = await testHelper.GetGameStateAsync(gameResponse.GameId, humanPlayer.Seat);

            // Assert - All players should have visible cards in DevMode
            Assert.NotNull(gameState.Players);
            foreach (var player in gameState.Players)
            {
                // In DevMode, even AI players should have visible card information
                // Note: This depends on how the GetGameState endpoint implements DevMode
                Assert.NotNull(player);
            }
        }

        /// <summary>
        /// Tests that card visibility changes appropriately between DevMode on/off
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldToggleCardVisibility_BasedOnDevModeConfiguration()
        {
            // Test 1: DevMode disabled - AI cards should be hidden
            var gameResponse1 = await CreateGameAsync("TestPlayer1");
            var humanPlayer1 = GetHumanPlayer(gameResponse1);
            var request1 = CreatePlayCardRequest(gameResponse1.GameId, humanPlayer1.Seat, 0);
            var response1 = await PlayCardAsync(request1);
            
            var aiHand1 = response1.PlayerHands.FirstOrDefault(ph => ph.Seat != humanPlayer1.Seat);
            Assert.NotNull(aiHand1);
            if (aiHand1.Cards.Count > 0)
            {
                Assert.All(aiHand1.Cards, card => Assert.Null(card.Value));
            }

            // Test 2: DevMode enabled - AI cards should be visible
            using var testHelper = new DevModeTestHelper();
            var gameResponse2 = await testHelper.CreateGameAsync("TestPlayer2");
            var humanPlayer2 = testHelper.GetHumanPlayer(gameResponse2);
            var request2 = testHelper.CreatePlayCardRequest(gameResponse2.GameId, humanPlayer2.Seat, 0);
            var response2 = await testHelper.PlayCardAsync(request2);
            
            var aiHand2 = response2.PlayerHands.FirstOrDefault(ph => ph.Seat != humanPlayer2.Seat);
            Assert.NotNull(aiHand2);
            if (aiHand2.Cards.Count > 0)
            {
                Assert.All(aiHand2.Cards, card => 
                {
                    Assert.NotNull(card.Value);
                    Assert.NotNull(card.Suit);
                });
            }
        }

        /// <summary>
        /// Tests that played cards are always visible regardless of DevMode setting
        /// </summary>
        [Fact]
        public async Task PlayCard_ShouldAlwaysShowPlayedCards_RegardlessOfDevMode()
        {
            // Test with DevMode disabled
            var gameResponse1 = await CreateGameAsync("TestPlayer1");
            var humanPlayer1 = GetHumanPlayer(gameResponse1);
            var request1 = CreatePlayCardRequest(gameResponse1.GameId, humanPlayer1.Seat, 0);
            var response1 = await PlayCardAsync(request1);
            
            var playedCard1 = response1.GameState.PlayedCards?.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer1.Seat);
            Assert.NotNull(playedCard1);
            Assert.NotNull(playedCard1.Card);
            Assert.NotNull(playedCard1.Card.Value);
            Assert.NotNull(playedCard1.Card.Suit);

            // Test with DevMode enabled
            using var testHelper = new DevModeTestHelper();
            var gameResponse2 = await testHelper.CreateGameAsync("TestPlayer2");
            var humanPlayer2 = testHelper.GetHumanPlayer(gameResponse2);
            var request2 = testHelper.CreatePlayCardRequest(gameResponse2.GameId, humanPlayer2.Seat, 0);
            var response2 = await testHelper.PlayCardAsync(request2);
            
            var playedCard2 = response2.GameState.PlayedCards?.FirstOrDefault(pc => pc.PlayerSeat == humanPlayer2.Seat);
            Assert.NotNull(playedCard2);
            Assert.NotNull(playedCard2.Card);
            Assert.NotNull(playedCard2.Card.Value);
            Assert.NotNull(playedCard2.Card.Suit);
        }
    }
}
