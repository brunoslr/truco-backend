using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.API.Services;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Custom WebApplicationFactory for integration tests that provides test-specific service configurations
    /// without modifying the main application code
    /// </summary>
    public class CustomTestWebApplicationFactory<TStartup> : WebApplicationFactory<TStartup> where TStartup : class
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                // Override configuration for tests
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    {"FeatureFlags:DevMode", "false"},
                    {"FeatureFlags:AutoAiPlay", "true"},
                    {"GameSettings:AIPlayDelayMs", "0"},  // No delay for tests
                    {"GameSettings:NewHandDelayMs", "0"}  // No delay for tests
                });
            });

            builder.ConfigureServices(services =>
            {
                // Remove the existing IEventPublisher registration and replace with a mock
                services.RemoveAll<IEventPublisher>();
                  // Create a mock event publisher that doesn't try to resolve scoped services from root provider
                var mockEventPublisher = new Mock<IEventPublisher>();
                mockEventPublisher.Setup(x => x.PublishAsync(It.IsAny<IGameEvent>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask);
                services.AddSingleton(mockEventPublisher.Object);
                
                // Override logging to reduce noise in tests
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
            });
        }
    }
}
