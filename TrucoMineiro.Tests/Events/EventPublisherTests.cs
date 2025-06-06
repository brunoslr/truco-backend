using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
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
        public void SimpleTest_ShouldPass()
        {
            // Arrange & Act & Assert
            Assert.True(true);
        }

        [Fact]
        public void EventPublisher_ShouldNotBeNull()
        {
            // Assert
            Assert.NotNull(_eventPublisher);
        }

        [Fact]
        public async Task TestEventPublisher_ShouldCaptureEvents()
        {
            // Arrange
            var testPublisher = new TestEventPublisher();

            // Act - publish a simple mock event
            var mockEvent = new MockGameEvent(Guid.NewGuid());
            await testPublisher.PublishAsync(mockEvent);

            // Assert
            Assert.Single(testPublisher.PublishedEvents);
            Assert.Contains(mockEvent, testPublisher.PublishedEvents);
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
