using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests.Events
{
    /// <summary>
    /// Tests for the event publishing system
    /// </summary>
    public class EventPublisherTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly IEventPublisher _eventPublisher;

        public EventPublisherTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddSingleton<IEventPublisher, InMemoryEventPublisher>();
            
            _serviceProvider = services.BuildServiceProvider();
            _eventPublisher = _serviceProvider.GetRequiredService<IEventPublisher>();
        }

        [Fact]
        public void EventPublisher_ShouldNotBeNull()
        {
            // Assert
            Assert.NotNull(_eventPublisher);
        }

        [Fact]
        public void GetHandlerCount_WithNoHandlers_ShouldReturnZero()
        {
            // Act
            var count = _eventPublisher.GetHandlerCount<GameStartedEvent>();

            // Assert
            Assert.Equal(0, count);
        }

        [Fact]
        public async Task PublishAsync_WithValidGameStartedEvent_ShouldNotThrow()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var gameState = new GameState { Id = gameId.ToString() };
            var players = new List<Player>
            {
                new Player { Id = Guid.NewGuid().ToString(), Name = "Player 1", IsAI = false },
                new Player { Id = Guid.NewGuid().ToString(), Name = "Player 2", IsAI = true }
            };
            var gameEvent = new GameStartedEvent(gameId, gameState, players);

            // Act & Assert
            await _eventPublisher.PublishAsync(gameEvent);
        }

        [Fact]
        public async Task PublishAsync_WithNullEvent_ShouldNotThrow()
        {
            // Act & Assert
            await _eventPublisher.PublishAsync<GameStartedEvent>(null!);
        }

        [Fact]
        public async Task TestEventPublisher_ShouldCaptureGameStartedEvent()
        {
            // Arrange
            var testPublisher = new TestEventPublisher();
            var gameId = Guid.NewGuid();
            var gameState = new GameState { Id = gameId.ToString() };
            var players = new List<Player>
            {
                new Player { Id = Guid.NewGuid().ToString(), Name = "Player 1", IsAI = false }
            };
            var gameEvent = new GameStartedEvent(gameId, gameState, players);

            // Act
            await testPublisher.PublishAsync(gameEvent);

            // Assert
            Assert.Single(testPublisher.PublishedEvents);
            Assert.Contains(gameEvent, testPublisher.PublishedEvents);
            testPublisher.AssertEventPublished<GameStartedEvent>();
        }

        [Fact]
        public async Task TestEventPublisher_WithHandler_ShouldExecuteHandler()
        {
            // Arrange
            var testPublisher = new TestEventPublisher();
            var testHandler = new SimpleTestEventHandler<GameStartedEvent>();
            testPublisher.RegisterHandler(testHandler);

            var gameId = Guid.NewGuid();
            var gameState = new GameState { Id = gameId.ToString() };
            var players = new List<Player>();
            var gameEvent = new GameStartedEvent(gameId, gameState, players);

            // Act
            await testPublisher.PublishAsync(gameEvent);

            // Assert
            testHandler.AssertWasCalled();
            testHandler.AssertCallCount(1);
            Assert.Single(testHandler.HandledEvents);
            Assert.Equal(gameEvent, testHandler.HandledEvents.First());
        }

        [Fact]
        public async Task TestEventPublisher_WithMultipleEvents_ShouldCaptureAll()
        {
            // Arrange
            var testPublisher = new TestEventPublisher();
            var gameId1 = Guid.NewGuid();
            var gameId2 = Guid.NewGuid();
            
            var event1 = new GameStartedEvent(gameId1, new GameState { Id = gameId1.ToString() }, new List<Player>());
            var event2 = new GameStartedEvent(gameId2, new GameState { Id = gameId2.ToString() }, new List<Player>());

            // Act
            await testPublisher.PublishAsync(event1);
            await testPublisher.PublishAsync(event2);

            // Assert
            Assert.Equal(2, testPublisher.PublishedEvents.Count);
            Assert.Contains(event1, testPublisher.PublishedEvents);
            Assert.Contains(event2, testPublisher.PublishedEvents);
        }

        [Fact]
        public async Task TestEventPublisher_GetEventsForGame_ShouldReturnCorrectEvents()
        {
            // Arrange
            var testPublisher = new TestEventPublisher();
            var gameId1 = Guid.NewGuid();
            var gameId2 = Guid.NewGuid();
            
            var event1 = new GameStartedEvent(gameId1, new GameState { Id = gameId1.ToString() }, new List<Player>());
            var event2 = new GameStartedEvent(gameId2, new GameState { Id = gameId2.ToString() }, new List<Player>());
            var event3 = new GameCompletedEvent(gameId1, null, new Dictionary<Guid, int>(), new GameState { Id = gameId1.ToString() }, TimeSpan.Zero);

            // Act
            await testPublisher.PublishAsync(event1);
            await testPublisher.PublishAsync(event2);
            await testPublisher.PublishAsync(event3);

            // Assert
            var game1Events = testPublisher.GetEventsForGame(gameId1);
            var game2Events = testPublisher.GetEventsForGame(gameId2);

            Assert.Equal(2, game1Events.Count);
            Assert.Single(game2Events);
            Assert.Contains(event1, game1Events);
            Assert.Contains(event3, game1Events);
            Assert.Contains(event2, game2Events);
        }

        [Fact]
        public void GameStartedEvent_ShouldHaveCorrectProperties()
        {
            // Arrange
            var gameId = Guid.NewGuid();
            var gameState = new GameState { Id = gameId.ToString() };
            var players = new List<Player>
            {
                new Player { Id = Guid.NewGuid().ToString(), Name = "Test Player", IsAI = false }
            };
            var startedBy = players.First();

            // Act
            var gameEvent = new GameStartedEvent(gameId, gameState, players, startedBy);

            // Assert
            Assert.Equal(gameId, gameEvent.GameId);
            Assert.Equal(gameState, gameEvent.GameState);
            Assert.Equal(players, gameEvent.Players);
            Assert.Equal(startedBy, gameEvent.StartedBy);
            Assert.Equal("GameStartedEvent", gameEvent.EventType);
            Assert.Equal(1, gameEvent.Version);
        }

        [Fact]
        public void MockGameEvent_ShouldInheritFromGameEventBase()
        {
            // Arrange
            var gameId = Guid.NewGuid();

            // Act
            var mockEvent = new MockGameEvent(gameId);

            // Assert
            Assert.Equal(gameId, mockEvent.GameId);
            Assert.Equal("MockGameEvent", mockEvent.EventType);
            Assert.True(mockEvent.OccurredAt > DateTime.UtcNow.AddMinutes(-1));
        }

        public void Dispose()
        {
            _serviceProvider?.Dispose();
        }
    }

    /// <summary>
    /// Simple mock event for testing
    /// </summary>
    public class MockGameEvent : GameEventBase
    {
        public MockGameEvent(Guid gameId) : base(gameId)
        {
        }
    }
}
