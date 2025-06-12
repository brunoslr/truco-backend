using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Repositories;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Integration tests for round flow event handling.
    /// Tests real event publishing and service interactions without complex mocking.
    /// 
    /// TESTING STRATEGY:
    /// - Uses real services with in-memory implementations
    /// - Captures actual events published through TestEventPublisher
    /// - Tests complete round flow scenarios end-to-end
    /// - Validates event sequences and game state transitions
    /// </summary>
    public class RoundFlowIntegrationTestsFixed : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly RoundFlowEventHandler _roundFlowHandler;
        private readonly TestEventPublisher _eventPublisher;
        private readonly InMemoryGameRepository _gameRepository;
        private readonly IGameStateManager _gameStateManager;
        private readonly IHandResolutionService _handResolutionService;

        public RoundFlowIntegrationTestsFixed()
        {
            var services = new ServiceCollection();
            
            // Configure fast test settings (zero delays)
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"GameSettings:RoundResolutionDelayMs", "0"},
                    {"GameSettings:HandResolutionDelayMs", "0"},
                    {"FeatureFlags:DevMode", "false"}
                })
                .Build();

            services.AddSingleton<IConfiguration>(configuration);
            services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
            
            // Register real services
            services.AddScoped<IGameRepository, InMemoryGameRepository>();
            services.AddScoped<IEventPublisher, TestEventPublisher>();
            services.AddScoped<IGameStateManager, GameStateManager>();
            services.AddScoped<IHandResolutionService, HandResolutionService>();
            services.AddScoped<RoundFlowEventHandler>();

            _serviceProvider = services.BuildServiceProvider();
            _roundFlowHandler = _serviceProvider.GetRequiredService<RoundFlowEventHandler>();
            _eventPublisher = (TestEventPublisher)_serviceProvider.GetRequiredService<IEventPublisher>();
            _gameRepository = (InMemoryGameRepository)_serviceProvider.GetRequiredService<IGameRepository>();
            _gameStateManager = _serviceProvider.GetRequiredService<IGameStateManager>();
            _handResolutionService = _serviceProvider.GetRequiredService<IHandResolutionService>();
        }

        [Fact]
        public async Task RoundFlow_WhenAllCardsPlayedAndHandComplete_ShouldPublishHandCompletedEvent()
        {
            // Arrange
            var game = CreateGameWithAllCardsPlayed();
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();
            
            // Setup a winning scenario: Team 1 wins 2 rounds (hand complete)
            game.RoundWinners.AddRange(new[] { 1, 1 });
            
            await _gameRepository.SaveGameAsync(game);
            
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert
            var publishedEvents = _eventPublisher.PublishedEvents;
            
            // Should publish RoundCompletedEvent first
            Assert.Contains(publishedEvents, e => e is RoundCompletedEvent);
            
            // Should publish HandCompletedEvent since all cards are played and hand is complete
            var handCompletedEvent = publishedEvents.OfType<HandCompletedEvent>().FirstOrDefault();
            Assert.NotNull(handCompletedEvent);
            Assert.Equal(gameId, handCompletedEvent.GameId);
            Assert.NotNull(handCompletedEvent.WinningTeam);
        }

        [Fact]
        public async Task RoundFlow_WhenAllCardsPlayedButHandNotComplete_ShouldNotPublishHandCompletedEvent()
        {
            // Arrange
            var game = CreateGameWithAllCardsPlayed();
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();
            
            // Setup scenario where hand is not complete: only 1 round won
            game.RoundWinners.Add(1);
            
            await _gameRepository.SaveGameAsync(game);
            
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert
            var publishedEvents = _eventPublisher.PublishedEvents;
            
            // Should publish RoundCompletedEvent
            Assert.Contains(publishedEvents, e => e is RoundCompletedEvent);
            
            // Should NOT publish HandCompletedEvent since hand is not complete
            Assert.DoesNotContain(publishedEvents, e => e is HandCompletedEvent);
        }

        [Fact]
        public async Task RoundFlow_WhenNotAllCardsPlayed_ShouldPublishPlayerTurnStartedEvent()
        {
            // Arrange
            var game = CreateGameWithSomeCardsRemaining();
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();
            
            await _gameRepository.SaveGameAsync(game);
            
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert
            var publishedEvents = _eventPublisher.PublishedEvents;
            
            // Should publish RoundCompletedEvent first
            Assert.Contains(publishedEvents, e => e is RoundCompletedEvent);
            
            // Should publish PlayerTurnStartedEvent for next round since not all cards are played
            var playerTurnEvent = publishedEvents.OfType<PlayerTurnStartedEvent>().FirstOrDefault();
            Assert.NotNull(playerTurnEvent);
            Assert.Equal(gameId, playerTurnEvent.GameId);
            Assert.NotNull(playerTurnEvent.Player);
            Assert.Contains("play-card", playerTurnEvent.AvailableActions);
        }

        [Fact]
        public async Task RoundFlow_WhenRoundNotComplete_ShouldPublishNextPlayerTurnEvent()
        {
            // Arrange
            var game = CreateGameWithSomeCardsRemaining();
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();
            
            // Setup game state where not all players have played in current round
            game.PlayedCards.Clear();
            game.PlayedCards.Add(new PlayedCard(0, new Card("A", "Spades"))); // Only one player played
            game.Players[1].IsActive = true; // Next player should be active
            
            await _gameRepository.SaveGameAsync(game);
            
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert
            var publishedEvents = _eventPublisher.PublishedEvents;
            
            // Should publish PlayerTurnStartedEvent for next player in same round
            var playerTurnEvent = publishedEvents.OfType<PlayerTurnStartedEvent>().FirstOrDefault();
            Assert.NotNull(playerTurnEvent);
            Assert.Equal(gameId, playerTurnEvent.GameId);
            
            // Should NOT publish RoundCompletedEvent since round is not complete
            Assert.DoesNotContain(publishedEvents, e => e is RoundCompletedEvent);
        }

        [Fact]
        public async Task RoundFlow_EventSequence_ShouldBeCorrect()
        {
            // Arrange
            var game = CreateGameWithSomeCardsRemaining();
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();
            
            await _gameRepository.SaveGameAsync(game);
            
            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert
            var publishedEvents = _eventPublisher.PublishedEvents.ToList();
            
            // Verify event sequence: RoundCompletedEvent should come before PlayerTurnStartedEvent
            var roundCompletedIndex = publishedEvents.FindIndex(e => e is RoundCompletedEvent);
            var playerTurnStartedIndex = publishedEvents.FindIndex(e => e is PlayerTurnStartedEvent);
            
            Assert.True(roundCompletedIndex >= 0, "RoundCompletedEvent should be published");
            Assert.True(playerTurnStartedIndex >= 0, "PlayerTurnStartedEvent should be published");
            Assert.True(roundCompletedIndex < playerTurnStartedIndex, "RoundCompletedEvent should come before PlayerTurnStartedEvent");
        }

        private static GameState CreateGameWithAllCardsPlayed()
        {
            return TestGameFactory.CreateGameWithAllCardsPlayed();
        }

        private static GameState CreateGameWithSomeCardsRemaining()
        {
            return TestGameFactory.CreateGameWithSomeCardsRemaining();
        }

        private static CardPlayedEvent CreateCardPlayedEvent(Guid gameId, GameState game)
        {
            var player = game.Players[0];
            var card = new Card("A", "Spades");
            
            return new CardPlayedEvent(
                gameId,
                player.Id,
                card,
                player,
                game.CurrentRound,
                game.CurrentHand,
                false,
                game);
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }
}
