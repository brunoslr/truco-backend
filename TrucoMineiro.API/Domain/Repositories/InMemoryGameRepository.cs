using System.Collections.Concurrent;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Domain.Repositories
{
    /// <summary>
    /// In-memory implementation of game repository
    /// </summary>
    public class InMemoryGameRepository : IGameRepository
    {
        private readonly ConcurrentDictionary<string, GameState> _games = new();

        public Task<bool> SaveGameAsync(GameState game)
        {
            try
            {
                game.LastActivity = DateTime.UtcNow;
                _games.AddOrUpdate(game.GameId, game, (key, oldValue) => game);
                return Task.FromResult(true);
            }
            catch
            {
                return Task.FromResult(false);
            }
        }

        public Task<GameState?> GetGameAsync(string gameId)
        {
            _games.TryGetValue(gameId, out var game);
            return Task.FromResult(game);
        }

        public Task<bool> RemoveGameAsync(string gameId)
        {
            var removed = _games.TryRemove(gameId, out _);
            return Task.FromResult(removed);
        }

        public Task<IEnumerable<GameState>> GetAllGamesAsync()
        {
            return Task.FromResult(_games.Values.AsEnumerable());
        }

        public Task<IEnumerable<GameState>> GetExpiredGamesAsync(int timeoutMinutes)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-timeoutMinutes);
            var expiredGames = _games.Values.Where(g => g.LastActivity < cutoffTime);
            return Task.FromResult(expiredGames);
        }

        public Task<bool> UpdateLastActivityAsync(string gameId)
        {
            if (_games.TryGetValue(gameId, out var game))
            {
                game.LastActivity = DateTime.UtcNow;
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> DeleteGameAsync(string gameId)
        {
            var removed = _games.TryRemove(gameId, out _);
            return Task.FromResult(removed);
        }
    }
}
