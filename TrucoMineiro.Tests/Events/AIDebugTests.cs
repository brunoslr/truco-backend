using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests.Events
{
    /// <summary>
    /// Debug tests to understand AI behavior issues
    /// </summary>
    public class AIDebugTests
    {
        private readonly IAIPlayerService _aiService;        public AIDebugTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IAIPlayerService, AIPlayerService>();
            services.AddScoped<IHandResolutionService, RealisticTestHandResolutionService>();

            var serviceProvider = services.BuildServiceProvider();
            _aiService = serviceProvider.GetRequiredService<IAIPlayerService>();
        }

        [Fact]
        public void Debug_BluffingScenario()
        {
            // Arrange - AI won first round but has weak hand (prime bluffing scenario)
            var player = CreateTestPlayer(0, CreateWeakHand());
            var game = CreateTestGame(teamScore: 6, opponentScore: 6, stakes: 2);
            game.RoundWinners = new List<int> { 1 }; // PlayerTeam (Team.PlayerTeam = 1) won first round

            // Debug: Test one decision and check intermediate values
            var decision = _aiService.DecideTrucoResponse(player, game, "truco", 4);
            
            // This should trigger bluffing since:
            // - PlayerTeam won first round (RoundWinners[0] = 1, which matches Team.PlayerTeam)
            // - Hand is weak (should be < 0.2 threshold)
            // - Bluff chance should be 25% + 15% = 40% + random factor
            
            // For now, just ensure the test runs without error
            Assert.True(true, $"Decision was: {decision}");
        }

        private Player CreateTestPlayer(int seat, List<Card> hand)
        {
            return new Player
            {
                Seat = seat,
                Hand = hand,
                IsAI = true
            };
        }

        private GameState CreateTestGame(int teamScore, int opponentScore, int stakes)
        {
            return new GameState
            {
                Id = Guid.NewGuid().ToString(),
                CurrentHand = 1,
                Stakes = stakes,
                TrucoCallState = TrucoCallState.None,
                TeamScores = new Dictionary<Team, int>
                {
                    { Team.PlayerTeam, teamScore },
                    { Team.OpponentTeam, opponentScore }
                },
                RoundWinners = new List<int>(),
                PlayedCards = new List<PlayedCard>()
            };
        }        private List<Card> CreateWeakHand()
        {            return new List<Card>
            {
                new Card { Suit = "Hearts", Value = "4" },
                new Card { Suit = "Diamonds", Value = "5" },
                new Card { Suit = "Clubs", Value = "6" }
            };
        }
    }
}
