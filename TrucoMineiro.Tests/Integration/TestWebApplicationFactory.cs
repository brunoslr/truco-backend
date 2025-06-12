using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Services;

namespace TrucoMineiro.Tests.Integration
{    /// <summary>
    /// Test web application factory for integration tests
    /// Uses real event publisher instead of mocks to test event-driven architecture
    /// Provides optional configuration overrides for test scenarios
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        private Dictionary<string, string?>? _configOverrides;

        public TestWebApplicationFactory()
        {
            _configOverrides = null;
        }

        /// <summary>
        /// Creates a new factory with configuration overrides
        /// </summary>
        public static TestWebApplicationFactory WithConfig(Dictionary<string, string?> configOverrides)
        {
            var factory = new TestWebApplicationFactory();
            factory._configOverrides = configOverrides;
            return factory;
        }        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            // Apply configuration overrides if provided
            // All test configuration is now code-based for better maintainability
            if (_configOverrides != null)
            {
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(_configOverrides);
                });
            }

            builder.ConfigureServices(services =>
            {
                // Use real event publisher for integration tests
                // This enables testing the complete event-driven flow
                // including AI auto-play functionality
                
                // Reduce logging noise in tests
                services.AddLogging(builder => builder.SetMinimumLevel(LogLevel.Warning));
                
                // All other services remain as configured in Program.cs
                // This allows testing the real application behavior
            });
        }
    }
}