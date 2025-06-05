using TrucoMineiro.API.Domain.Interfaces;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Background service for cleaning up expired games every 5 minutes
    /// </summary>
    public class GameCleanupService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<GameCleanupService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public GameCleanupService(IServiceProvider serviceProvider, ILogger<GameCleanupService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Game cleanup service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await CleanupExpiredGames();
                    await Task.Delay(_cleanupInterval, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during game cleanup");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Wait 1 minute before retrying
                }
            }

            _logger.LogInformation("Game cleanup service stopped");
        }

        private async Task CleanupExpiredGames()
        {
            using var scope = _serviceProvider.CreateScope();
            var gameStateManager = scope.ServiceProvider.GetRequiredService<IGameStateManager>();

            try
            {
                var expiredGameIds = await gameStateManager.GetExpiredGameIdsAsync();
                
                if (expiredGameIds.Count > 0)
                {
                    _logger.LogInformation($"Found {expiredGameIds.Count} expired games to clean up");
                    await gameStateManager.CleanupExpiredGamesAsync();
                    _logger.LogInformation($"Successfully cleaned up {expiredGameIds.Count} expired games");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup expired games");
            }
        }
    }
}
