using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.Tests.Events;
using TrucoMineiro.Tests.TestUtilities;

namespace TrucoMineiro.Tests.Integration
{
    /// <summary>
    /// Integration tests to verify that the winner of a round becomes the first player in the next round
    /// This tests the Truco rule that the winner of the highest card in a round plays first in the next round
    /// </summary>
    public class RoundWinnerFirstPlayerTests : IDisposable
    {
        private readonly ServiceProvider _serviceProvider;
        private readonly RoundFlowEventHandler _roundFlowHandler;
        private readonly TestGameRepository _gameRepository;
        private readonly TestEventPublisher _eventPublisher;

        public RoundWinnerFirstPlayerTests()
        {
            var services = new ServiceCollection();

            // Add logging
            services.AddLogging(builder => builder.AddConsole());

            // Add test implementations
            services.AddScoped<IGameRepository, TestGameRepository>();
            services.AddScoped<IEventPublisher, TestEventPublisher>();
            services.AddScoped<IHandResolutionService, TestHandResolutionService>();
            services.AddScoped<IGameStateManager, GameStateManager>();

            // Add configuration for delays (set to 0 for tests)
            var configurationBuilder = new ConfigurationBuilder();
            configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
            {
                {"GameSettings:RoundResolutionDelayMs", "0"} // No delay for tests
            });
            var configuration = configurationBuilder.Build();
            services.AddSingleton<IConfiguration>(configuration);

            // Add the handler we're testing
            services.AddScoped<RoundFlowEventHandler>();

            _serviceProvider = services.BuildServiceProvider();
            _roundFlowHandler = _serviceProvider.GetRequiredService<RoundFlowEventHandler>();
            _gameRepository = (TestGameRepository)_serviceProvider.GetRequiredService<IGameRepository>();
            _eventPublisher = (TestEventPublisher)_serviceProvider.GetRequiredService<IEventPublisher>();
        }

        [Fact]
        public async Task Round2_ShouldStartWithRound1Winner()
        {
            // Arrange - Create game in round 1 where player 2 (seat 1) wins
            var game = CreateGameWithRound1Winner(winnerSeat: 1);
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();

            await _gameRepository.SaveGameAsync(game);

            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act - Trigger round completion and new round start
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert - Player 2 (seat 1) should be active for round 2
            var updatedGame = await _gameRepository.GetGameAsync(gameId.ToString());
            Assert.NotNull(updatedGame);
            Assert.Equal(2, updatedGame.CurrentRound); // Should be in round 2
            Assert.Equal(1, updatedGame.CurrentPlayerIndex); // Player 2 (seat 1) should be current player
            Assert.True(updatedGame.Players[1].IsActive); // Player 2 should be active
            Assert.False(updatedGame.Players[0].IsActive); // Player 1 should not be active
            Assert.False(updatedGame.Players[2].IsActive); // Player 3 should not be active
            Assert.False(updatedGame.Players[3].IsActive); // Player 4 should not be active

            // Verify PlayerTurnStartedEvent was published for the round winner
            var playerTurnEvent = _eventPublisher.PublishedEvents.OfType<PlayerTurnStartedEvent>().FirstOrDefault();
            Assert.NotNull(playerTurnEvent);
            Assert.Equal(1, playerTurnEvent.Player.Seat); // Should be player 2 (seat 1)
            Assert.Equal(2, playerTurnEvent.Round); // Should be round 2
        }

        [Fact]
        public async Task Round3_ShouldStartWithRound2Winner()
        {
            // Arrange - Create game in round 2 where player 4 (seat 3) wins
            var game = CreateGameWithRound2Winner(winnerSeat: 3);
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();

            await _gameRepository.SaveGameAsync(game);

            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act - Trigger round completion and new round start
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert - Player 4 (seat 3) should be active for round 3
            var updatedGame = await _gameRepository.GetGameAsync(gameId.ToString());
            Assert.NotNull(updatedGame);
            Assert.Equal(3, updatedGame.CurrentRound); // Should be in round 3
            Assert.Equal(3, updatedGame.CurrentPlayerIndex); // Player 4 (seat 3) should be current player
            Assert.True(updatedGame.Players[3].IsActive); // Player 4 should be active
            Assert.False(updatedGame.Players[0].IsActive); // Player 1 should not be active
            Assert.False(updatedGame.Players[1].IsActive); // Player 2 should not be active
            Assert.False(updatedGame.Players[2].IsActive); // Player 3 should not be active

            // Verify PlayerTurnStartedEvent was published for the round winner
            var playerTurnEvent = _eventPublisher.PublishedEvents.OfType<PlayerTurnStartedEvent>().FirstOrDefault();
            Assert.NotNull(playerTurnEvent);
            Assert.Equal(3, playerTurnEvent.Player.Seat); // Should be player 4 (seat 3)
            Assert.Equal(3, playerTurnEvent.Round); // Should be round 3
        }

        [Fact]
        public async Task Round2_WithDraw_ShouldStartWithFirstPlayer()
        {
            // Arrange - Create game where round 1 ends in a draw
            var game = CreateGameWithRound1Draw();
            var gameId = Guid.NewGuid();
            game.Id = gameId.ToString();

            await _gameRepository.SaveGameAsync(game);

            var cardPlayedEvent = CreateCardPlayedEvent(gameId, game);
            _eventPublisher.Clear();

            // Act - Trigger round completion and new round start
            await _roundFlowHandler.HandleAsync(cardPlayedEvent, CancellationToken.None);

            // Assert - First player (seat 0) should be active for round 2 when there's a draw
            var updatedGame = await _gameRepository.GetGameAsync(gameId.ToString());
            Assert.NotNull(updatedGame);
            Assert.Equal(2, updatedGame.CurrentRound); // Should be in round 2
            Assert.Equal(0, updatedGame.CurrentPlayerIndex); // First player (seat 0) should be current player
            Assert.True(updatedGame.Players[0].IsActive); // First player should be active
            Assert.False(updatedGame.Players[1].IsActive);
            Assert.False(updatedGame.Players[2].IsActive);
            Assert.False(updatedGame.Players[3].IsActive);

            // Verify PlayerTurnStartedEvent was published for the first player
            var playerTurnEvent = _eventPublisher.PublishedEvents.OfType<PlayerTurnStartedEvent>().FirstOrDefault();
            Assert.NotNull(playerTurnEvent);
            Assert.Equal(0, playerTurnEvent.Player.Seat); // Should be first player (seat 0)
            Assert.Equal(2, playerTurnEvent.Round); // Should be round 2
        }

        private static GameState CreateGameWithRound1Winner(int winnerSeat)
        {
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 1;

            // Set up all cards played in round 1 with specific winner
            game.PlayedCards.Clear();
            game.PlayedCards.Add(new PlayedCard(0, new Card("7", "♠"))); // Weak card
            game.PlayedCards.Add(new PlayedCard(1, winnerSeat == 1 ? new Card("A", "♠") : new Card("8", "♠"))); // Strong if winner
            game.PlayedCards.Add(new PlayedCard(2, new Card("9", "♠"))); // Medium card
            game.PlayedCards.Add(new PlayedCard(3, winnerSeat == 3 ? new Card("A", "♠") : new Card("10", "♠"))); // Strong if winner

            // Set all players as having cards remaining (not hand complete)
            foreach (var player in game.Players)
            {
                player.Hand = new List<Card> { new Card("K", "♥"), new Card("Q", "♦") }; // 2 cards remaining
            }

            return game;
        }

        private static GameState CreateGameWithRound2Winner(int winnerSeat)
        {
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 2; // Already in round 2
            game.RoundWinners.Add(1); // Team 1 won round 1

            // Set up all cards played in round 2 with specific winner
            game.PlayedCards.Clear();
            game.PlayedCards.Add(new PlayedCard(0, new Card("7", "♥"))); // Weak card
            game.PlayedCards.Add(new PlayedCard(1, new Card("8", "♥"))); // Medium card
            game.PlayedCards.Add(new PlayedCard(2, new Card("9", "♥"))); // Medium card
            game.PlayedCards.Add(new PlayedCard(3, winnerSeat == 3 ? new Card("A", "♥") : new Card("10", "♥"))); // Strong if winner

            // Set all players as having cards remaining (not hand complete)
            foreach (var player in game.Players)
            {
                player.Hand = new List<Card> { new Card("K", "♣") }; // 1 card remaining
            }

            return game;
        }

        private static GameState CreateGameWithRound1Draw()
        {
            var game = new GameState();
            game.InitializeGame("TestPlayer");
            game.CurrentRound = 1;

            // Set up round 1 ending in a draw (same card values)
            game.PlayedCards.Clear();
            game.PlayedCards.Add(new PlayedCard(0, new Card("A", "♠"))); // Same strength
            game.PlayedCards.Add(new PlayedCard(1, new Card("A", "♥"))); // Same strength -> draw
            game.PlayedCards.Add(new PlayedCard(2, new Card("7", "♠"))); // Weaker card
            game.PlayedCards.Add(new PlayedCard(3, new Card("8", "♠"))); // Weaker card

            // Set all players as having cards remaining (not hand complete)
            foreach (var player in game.Players)
            {
                player.Hand = new List<Card> { new Card("K", "♥"), new Card("Q", "♦") }; // 2 cards remaining
            }

            return game;
        }

        private static CardPlayedEvent CreateCardPlayedEvent(Guid gameId, GameState game)
        {
            var player = game.Players[0];
            var card = new Card("J", "♠");

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

    /// <summary>
    /// Test implementation of HandResolutionService that can determine winners based on card values
    /// </summary>
    public class TestHandResolutionService : IHandResolutionService
    {
        public int GetCardStrength(Card card)
        {
            // Simple card strength mapping for tests
            return card.Value switch
            {
                "A" => 14,
                "K" => 13,
                "Q" => 12,
                "J" => 11,
                "10" => 10,
                "9" => 9,
                "8" => 8,
                "7" => 7,
                _ => 1
            };
        }

        public Player? DetermineRoundWinner(List<PlayedCard> playedCards, List<Player> players)
        {
            if (!playedCards.Any(pc => !pc.Card.IsEmpty))
                return null;

            Player? winner = null;
            int highestStrength = -1;
            bool hasDraw = false;

            foreach (var playedCard in playedCards.Where(pc => !pc.Card.IsEmpty))
            {
                var player = players.FirstOrDefault(p => p.Seat == playedCard.PlayerSeat);
                if (player != null && !playedCard.Card.IsEmpty)
                {
                    var strength = GetCardStrength(playedCard.Card);

                    if (strength > highestStrength)
                    {
                        highestStrength = strength;
                        winner = player;
                        hasDraw = false;
                    }
                    else if (strength == highestStrength)
                    {
                        hasDraw = true;
                    }
                }
            }

            return hasDraw ? null : winner;
        }

        public bool IsRoundDraw(List<PlayedCard> playedCards, List<Player> players) =>
            DetermineRoundWinner(playedCards, players) == null;

        public string? HandleDrawResolution(GameState game, int roundNumber) => null;

        public bool IsHandComplete(GameState game) => false;

        public Team? GetHandWinner(GameState game) => null;

        public Player? DetermineHandWinner(GameState game) => null;

        public void UpdateGameScores(GameState game, Player handWinner, int points) { }

        public Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs) => Task.CompletedTask;
    }
}
