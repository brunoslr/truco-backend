using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests.Events
{
    /// <summary>
    /// Integration tests for event-driven AI players
    /// </summary>
    public class EventDrivenAIPlayerTests
    {
        [Fact]
        public async Task AIPlayerEventHandler_Should_Handle_PlayerTurnStartedEvent()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IGameRepository, TestGameRepository>();
            services.AddScoped<IEventPublisher, TestEventPublisher>();
            services.AddScoped<IAIPlayerService, TestAIPlayerService>();
            services.AddScoped<IGameStateManager, TestGameStateManager>();
            services.AddScoped<AIPlayerEventHandler>();

            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetRequiredService<AIPlayerEventHandler>();
            var gameRepo = (TestGameRepository)serviceProvider.GetRequiredService<IGameRepository>();
            var eventPublisher = (TestEventPublisher)serviceProvider.GetRequiredService<IEventPublisher>();            // Create a test game with AI player
            var gameId = Guid.NewGuid();
            var game = new GameState
            {
                Id = gameId.ToString(), // Use Id property instead of GameId
                CurrentHand = 1,
                CurrentRound = 1,
                CurrentPlayerIndex = 1,
                Players = new List<Player>
                {
                    new Player("Human", "team1", 0) { IsAI = false },
                    new Player("AI Player", "team2", 1) { IsAI = true, IsActive = true },
                    new Player("Partner", "team1", 2) { IsAI = false },
                    new Player("AI Partner", "team2", 3) { IsAI = true }
                }
            };

            // Initialize player hands
            foreach (var player in game.Players)
            {
                player.Hand = new List<Card>
                {
                    new Card("♠", "A"),
                    new Card("♥", "K"),
                    new Card("♦", "Q")
                };
            }

            gameRepo.AddGame(game);

            var playerTurnEvent = new PlayerTurnStartedEvent(
                gameId,
                game.Players[1], // AI player
                game.CurrentRound,
                game.CurrentHand,
                game,
                new List<string> { "play-card" }
            );

            // Act
            await handler.HandleAsync(playerTurnEvent);

            // Assert
            Assert.True(eventPublisher.PublishedEvents.Count > 0);
            Assert.Contains(eventPublisher.PublishedEvents, e => e is CardPlayedEvent);

            var cardPlayedEvent = eventPublisher.PublishedEvents.OfType<CardPlayedEvent>().First();
            Assert.Equal(gameId, cardPlayedEvent.GameId);
            Assert.Equal(1, cardPlayedEvent.Player.Seat); // AI player seat
            Assert.NotNull(cardPlayedEvent.Card);
        }

        [Fact]
        public async Task GameFlowEventHandler_Should_Handle_CardPlayedEvent()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IGameRepository, TestGameRepository>();
            services.AddScoped<IEventPublisher, TestEventPublisher>();
            services.AddScoped<IGameFlowService, TestGameFlowService>();
            services.AddScoped<IHandResolutionService, TestHandResolutionService>();
            services.AddScoped<GameFlowEventHandler>();

            var serviceProvider = services.BuildServiceProvider();
            var handler = serviceProvider.GetRequiredService<GameFlowEventHandler>();
            var gameRepo = (TestGameRepository)serviceProvider.GetRequiredService<IGameRepository>();
            var eventPublisher = (TestEventPublisher)serviceProvider.GetRequiredService<IEventPublisher>();            // Create a test game
            var gameId = Guid.NewGuid();
            var game = new GameState
            {
                Id = gameId.ToString(), // Use Id property instead of GameId
                CurrentHand = 1,
                CurrentRound = 1,
                CurrentPlayerIndex = 1,
                Players = new List<Player>
                {
                    new Player("Human", "team1", 0) { IsAI = false },
                    new Player("AI Player", "team2", 1) { IsAI = true },
                    new Player("Partner", "team1", 2) { IsAI = false, IsActive = true },
                    new Player("AI Partner", "team2", 3) { IsAI = true }
                }
            };

            gameRepo.AddGame(game);            var cardPlayedEvent = new CardPlayedEvent(
                gameId,
                game.Players[1].Id, // Player ID as Guid (already a Guid)
                new Card("♠", "A"),
                game.Players[1], // AI player who just played
                game.CurrentRound,
                game.CurrentHand,
                true, // isAIMove
                game
            );

            // Act
            await handler.HandleAsync(cardPlayedEvent);

            // Assert - Should trigger next player turn or round completion
            Assert.True(eventPublisher.PublishedEvents.Count > 0);
        }
    }    // Test implementations
    public class TestGameRepository : IGameRepository
    {
        private readonly Dictionary<string, GameState> _games = new();

        public void AddGame(GameState game) => _games[game.GameId] = game;

        public Task<GameState?> GetGameAsync(string gameId) => 
            Task.FromResult(_games.TryGetValue(gameId, out var game) ? game : null);

        public Task<bool> SaveGameAsync(GameState game)
        {
            _games[game.GameId] = game;
            return Task.FromResult(true);
        }

        public Task<bool> RemoveGameAsync(string gameId)
        {
            var removed = _games.Remove(gameId);
            return Task.FromResult(removed);
        }

        public Task<IEnumerable<GameState>> GetAllGamesAsync() => 
            Task.FromResult(_games.Values.AsEnumerable());

        public Task<IEnumerable<GameState>> GetExpiredGamesAsync(int timeoutMinutes) => 
            Task.FromResult(Enumerable.Empty<GameState>());

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
            var removed = _games.Remove(gameId);
            return Task.FromResult(removed);
        }
    }    public class TestAIPlayerService : IAIPlayerService
    {
        public Task<Card> MakeDecisionAsync(GameState game, Player aiPlayer, CancellationToken cancellationToken = default)
        {
            var availableCard = aiPlayer.Hand.FirstOrDefault() ?? new Card("♠", "A");
            return Task.FromResult(availableCard);
        }

        public int SelectCardToPlay(Player player, GameState game)
        {
            return 0; // Always select first card
        }

        public bool ShouldCallTruco(Player player, GameState game)
        {
            return false; // Never call Truco in tests
        }

        public bool ShouldRaise(Player player, GameState game)
        {
            return false; // Never raise in tests
        }        public bool ShouldFold(Player player, GameState game)
        {
            return false; // Never fold in tests
        }

        public bool IsAIPlayer(Player player)
        {
            return player.IsAI;
        }
    }

    public class TestGameStateManager : IGameStateManager
    {
        public void PlayCard(GameState game, int playerSeat, Card card)
        {
            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player != null)
            {
                player.Hand.Remove(card);
                game.PlayedCards.Add(new PlayedCard(playerSeat, card));
            }
        }

        public void AdvanceToNextPlayer(GameState game)
        {
            var currentPlayer = game.Players.FirstOrDefault(p => p.IsActive);
            if (currentPlayer != null)
            {
                currentPlayer.IsActive = false;
                var nextPlayerIndex = (currentPlayer.Seat + 1) % game.Players.Count;
                game.Players[nextPlayerIndex].IsActive = true;
                game.CurrentPlayerIndex = nextPlayerIndex;
            }
        }

        public Task<GameState> CreateGameAsync(string? playerName = null)
        {
            var game = new GameState();
            game.InitializeGame(playerName ?? "Test Player");
            return Task.FromResult(game);
        }

        public Task<GameState?> GetActiveGameAsync(string gameId)
        {
            return Task.FromResult<GameState?>(null);
        }

        public Task<bool> SaveGameAsync(GameState game)
        {
            return Task.FromResult(true);
        }

        public Task<bool> RemoveGameAsync(string gameId)
        {
            return Task.FromResult(true);
        }

        public Task<int> CleanupExpiredGamesAsync()
        {
            return Task.FromResult(0);
        }

        public Task<int> CleanupCompletedGamesAsync()
        {
            return Task.FromResult(0);
        }

        public bool IsGameExpired(GameState game)
        {
            return false;
        }

        public bool IsGameCompleted(GameState game)
        {
            return game.IsCompleted;
        }

        public Task<List<string>> GetExpiredGameIdsAsync()
        {
            return Task.FromResult(new List<string>());
        }
    }    public class TestGameFlowService : IGameFlowService
    {
        public bool PlayCard(GameState game, int playerSeat, int cardIndex)
        {
            return true;
        }

        public Task ProcessAITurnsAsync(GameState game, int aiPlayDelayMs)
        {
            return Task.CompletedTask;
        }

        public Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs)
        {
            return Task.CompletedTask;
        }

        public void AdvanceToNextPlayer(GameState game) { }
        
        public bool IsRoundComplete(GameState game) => false;
        
        public void StartNewHand(GameState game) { }
    }    public class TestHandResolutionService : IHandResolutionService
    {
        public int GetCardStrength(Card card) => 5;
        
        public Player? DetermineRoundWinner(List<PlayedCard> playedCards, List<Player> players) => 
            players.FirstOrDefault();
        
        public bool IsRoundDraw(List<PlayedCard> playedCards, List<Player> players) => false;
        
        public string? HandleDrawResolution(GameState game, int roundNumber) => null;
        
        public bool IsHandComplete(GameState game) => false;
        
        public string? GetHandWinner(GameState game) => null;
        
        public Player? DetermineHandWinner(GameState game) => null;
        
        public void UpdateGameScores(GameState game, Player handWinner, int points) { }
    }
}
