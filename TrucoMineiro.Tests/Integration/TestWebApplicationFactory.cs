using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using TrucoMineiro.API;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Services;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Test web application factory for integration tests
    /// Uses real event publisher instead of mocks to test event-driven architecture
    /// </summary>
    public class TestWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Use real event publisher for integration tests
                // This enables testing the complete event-driven flow
                // including AI auto-play functionality
                
                // All other services remain as configured in Program.cs
                // This allows testing the real application behavior
            });
        }
    }
}