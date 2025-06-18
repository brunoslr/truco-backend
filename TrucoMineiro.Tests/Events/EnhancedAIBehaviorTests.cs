using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.Services;
using TrucoMineiro.Tests.TestUtilities;
using Xunit;

namespace TrucoMineiro.Tests.Events
{
    /// <summary>
    /// Tests for enhanced AI behavior including bluffing, victory-awareness, and strategic decision making
    /// </summary>
    public class EnhancedAIBehaviorTests
    {
        private readonly IAIPlayerService _aiService;
        private readonly IHandResolutionService _handResolutionService;        public EnhancedAIBehaviorTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(builder => builder.AddConsole());
            services.AddScoped<IAIPlayerService, AIPlayerService>();
            services.AddScoped<IHandResolutionService, RealisticTestHandResolutionService>();

            var serviceProvider = services.BuildServiceProvider();
            _aiService = serviceProvider.GetRequiredService<IAIPlayerService>();
            _handResolutionService = serviceProvider.GetRequiredService<IHandResolutionService>();
        }[Fact]
        public void DecideTrucoResponse_Should_Never_Surrender_When_Enemy_Will_Win()
        {
            // Arrange - Enemy is about to win (11 points, stakes is 2)
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 5, opponentScore: 11, stakes: 2);

            // Act
            var decision = _aiService.DecideTrucoResponse(player, game, "truco", 4);

            // Assert - Should never surrender when enemy will win
            Assert.NotEqual(TrucoDecision.Surrender, decision);
            Assert.True(decision == TrucoDecision.Accept || decision == TrucoDecision.Raise);
        }

        [Fact]
        public void DecideTrucoResponse_Should_Never_Raise_When_Own_Victory_Assured()
        {
            // Arrange - AI team is about to win (11 points, stakes is 2)
            var player = CreateTestPlayer(0, CreateStrongHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 11, opponentScore: 5, stakes: 2);

            // Act
            var decision = _aiService.DecideTrucoResponse(player, game, "truco", 4);

            // Assert - Should never raise when victory is assured, only accept or surrender
            Assert.NotEqual(TrucoDecision.Raise, decision);
            Assert.True(decision == TrucoDecision.Accept || decision == TrucoDecision.Surrender);
        }

        [Fact]
        public void ShouldCallTruco_Should_Never_Call_When_Victory_Assured()
        {
            // Arrange - AI team is about to win with current stakes
            var player = CreateTestPlayer(0, CreateStrongHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 11, opponentScore: 5, stakes: 2);

            // Act
            var shouldCall = _aiService.ShouldCallTruco(player, game);

            // Assert - Should never call truco when victory is assured with current stakes
            Assert.False(shouldCall);
        }

        [Fact]
        public void ShouldCallTruco_Should_Be_Aggressive_When_Enemy_About_To_Win()
        {
            // Arrange - Enemy is about to win
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 5, opponentScore: 11, stakes: 2);

            // Act - Test multiple times due to randomness
            var aggressiveCalls = 0;
            for (int i = 0; i < 100; i++)
            {
                if (_aiService.ShouldCallTruco(player, game))
                    aggressiveCalls++;
            }

            // Assert - Should be more aggressive (at least 40% call rate in desperation)
            Assert.True(aggressiveCalls > 40, $"Expected at least 40 aggressive calls out of 100, got {aggressiveCalls}");
        }

        [Fact]
        public void ShouldRaise_Should_Never_Raise_When_Victory_Assured()
        {
            // Arrange - AI team about to win
            var player = CreateTestPlayer(0, CreateStrongHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 11, opponentScore: 5, stakes: 2, trucoCallState: TrucoCallState.Truco);

            // Act
            var shouldRaise = _aiService.ShouldRaise(player, game);

            // Assert - Should never raise when victory is assured
            Assert.False(shouldRaise);
        }

        [Fact]
        public void ShouldRaise_Should_Be_Aggressive_When_Enemy_About_To_Win()
        {
            // Arrange - Enemy about to win
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 5, opponentScore: 11, stakes: 2, trucoCallState: TrucoCallState.Truco);

            // Act - Test multiple times due to randomness
            var aggressiveRaises = 0;
            for (int i = 0; i < 100; i++)
            {
                if (_aiService.ShouldRaise(player, game))
                    aggressiveRaises++;
            }

            // Assert - Should attempt desperate raises (at least 30% rate)
            Assert.True(aggressiveRaises > 30, $"Expected at least 30 aggressive raises out of 100, got {aggressiveRaises}");
        }

        [Fact]
        public void DecideTrucoResponse_Should_Show_Bluffing_Behavior()
        {
            // Arrange - AI won first round but has weak hand (prime bluffing scenario)
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 6, opponentScore: 6, stakes: 2);
            game.RoundWinners = new List<int> { 0 }; // PlayerTeam (0) won first round

            // Act - Test multiple times to observe bluffing behavior
            var raises = 0;
            var accepts = 0;
            var surrenders = 0;

            for (int i = 0; i < 100; i++)
            {
                var decision = _aiService.DecideTrucoResponse(player, game, "truco", 4);
                switch (decision)
                {
                    case TrucoDecision.Raise: raises++; break;
                    case TrucoDecision.Accept: accepts++; break;
                    case TrucoDecision.Surrender: surrenders++; break;
                }
            }

            // Assert - Should show some bluffing (occasional raises with weak hand)
            Assert.True(raises > 5, $"Expected some bluff raises, got {raises} out of 100");
            Assert.True(accepts > 0, "Should accept sometimes");
        }        [Fact]
        public void DecideTrucoResponse_Should_Be_More_Aggressive_When_Behind()
        {
            // Arrange - AI team is behind in score
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use weak hand to show aggression differences
            var gameBehind = CreateTestGame(teamScore: 4, opponentScore: 8, stakes: 2);
            var gameAhead = CreateTestGame(teamScore: 8, opponentScore: 4, stakes: 2);

            // Act - Test aggression when behind vs ahead
            var aggressiveWhenBehind = 0;
            var aggressiveWhenAhead = 0;

            for (int i = 0; i < 100; i++)
            {
                var decisionBehind = _aiService.DecideTrucoResponse(player, gameBehind, "truco", 4);
                var decisionAhead = _aiService.DecideTrucoResponse(player, gameAhead, "truco", 4);

                if (decisionBehind == TrucoDecision.Raise || decisionBehind == TrucoDecision.Accept)
                    aggressiveWhenBehind++;

                if (decisionAhead == TrucoDecision.Raise || decisionAhead == TrucoDecision.Accept)
                    aggressiveWhenAhead++;
            }

            // Assert - Should be more aggressive when behind
            Assert.True(aggressiveWhenBehind > aggressiveWhenAhead, 
                $"Expected more aggression when behind ({aggressiveWhenBehind}) than when ahead ({aggressiveWhenAhead})");
        }        [Fact]
        public void DecideTrucoResponse_Should_Apply_Won_First_Round_Bonus()
        {
            // Arrange - Test with and without first round win
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use weak hand to show aggression differences
            var gameWonFirst = CreateTestGame(teamScore: 6, opponentScore: 6, stakes: 2);
            gameWonFirst.RoundWinners = new List<int> { 0 }; // PlayerTeam (0) won first round

            var gameNoFirstWin = CreateTestGame(teamScore: 6, opponentScore: 6, stakes: 2);
            gameNoFirstWin.RoundWinners = new List<int> { 1 }; // OpponentTeam (1) won first round

            // Act - Test aggression with and without first round win
            var aggressiveWithFirstWin = 0;
            var aggressiveWithoutFirstWin = 0;

            for (int i = 0; i < 100; i++)
            {
                var decisionWithWin = _aiService.DecideTrucoResponse(player, gameWonFirst, "truco", 4);
                var decisionWithoutWin = _aiService.DecideTrucoResponse(player, gameNoFirstWin, "truco", 4);

                if (decisionWithWin == TrucoDecision.Raise || decisionWithWin == TrucoDecision.Accept)
                    aggressiveWithFirstWin++;

                if (decisionWithoutWin == TrucoDecision.Raise || decisionWithoutWin == TrucoDecision.Accept)
                    aggressiveWithoutFirstWin++;
            }

            // Assert - Should be more aggressive when won first round
            Assert.True(aggressiveWithFirstWin > aggressiveWithoutFirstWin,
                $"Expected more aggression with first round win ({aggressiveWithFirstWin}) than without ({aggressiveWithoutFirstWin})");
        }

        [Fact]
        public void DecideTrucoResponse_Should_Show_Randomness_In_Decisions()
        {
            // Arrange - Same scenario, should produce different decisions due to randomness
            var player = CreateTestPlayer(0, CreateMediumHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 6, opponentScore: 6, stakes: 2);

            // Act - Test same scenario multiple times
            var decisions = new List<TrucoDecision>();
            for (int i = 0; i < 50; i++)
            {
                var decision = _aiService.DecideTrucoResponse(player, game, "truco", 4);
                decisions.Add(decision);
            }

            // Assert - Should have variety in decisions (not all the same)
            var uniqueDecisions = decisions.Distinct().Count();
            Assert.True(uniqueDecisions > 1, "Expected variety in decisions due to randomness, but all were the same");
        }

        [Fact]
        public void ShouldCallTruco_Should_Show_Bluffing_With_Won_First_Round()
        {
            // Arrange - Won first round with weak hand
            var player = CreateTestPlayer(0, CreateWeakHand()); // Use seat 0 (PlayerTeam)
            var game = CreateTestGame(teamScore: 6, opponentScore: 6, stakes: 2);
            game.RoundWinners = new List<int> { 0 }; // PlayerTeam (0) won first round

            // Act - Test bluffing behavior
            var trucoCalls = 0;
            for (int i = 0; i < 100; i++)
            {
                if (_aiService.ShouldCallTruco(player, game))
                    trucoCalls++;
            }

            // Assert - Should sometimes bluff with weak hand when won first round
            Assert.True(trucoCalls > 10, $"Expected some bluff truco calls with weak hand, got {trucoCalls} out of 100");
        }

        // Helper methods for creating test data
        private Player CreateTestPlayer(int seat, List<Card> hand)
        {
            return new Player
            {
                Seat = seat,
                Hand = hand,
                IsAI = true
            };
        }        private GameState CreateTestGame(int teamScore, int opponentScore, int stakes, TrucoCallState trucoCallState = TrucoCallState.None)
        {
            var game = new GameState
            {
                Id = Guid.NewGuid().ToString(),
                CurrentHand = 1,
                Stakes = stakes,
                TrucoCallState = trucoCallState,
                TeamScores = new Dictionary<Team, int>
                {
                    { Team.PlayerTeam, teamScore },   // Seats 0,2 = PlayerTeam
                    { Team.OpponentTeam, opponentScore }  // Seats 1,3 = OpponentTeam
                },
                RoundWinners = new List<int>(),
                PlayedCards = new List<PlayedCard>()
            };

            return game;
        }

        private List<Card> CreateWeakHand()
        {
            return new List<Card>
            {
                new Card { Suit = "Hearts", Value = "4" },
                new Card { Suit = "Diamonds", Value = "5" },
                new Card { Suit = "Clubs", Value = "6" }
            };
        }

        private List<Card> CreateMediumHand()
        {
            return new List<Card>
            {
                new Card { Suit = "Hearts", Value = "10" },
                new Card { Suit = "Diamonds", Value = "Jack" },
                new Card { Suit = "Clubs", Value = "Queen" }
            };
        }        private List<Card> CreateStrongHand()
        {
            return new List<Card>
            {
                new Card { Suit = "Hearts", Value = "Ace" },
                new Card { Suit = "Diamonds", Value = "2" },
                new Card { Suit = "Clubs", Value = "3" }
            };
        }
    }

    /// <summary>
    /// Realistic test hand resolution service that provides appropriate card strengths for testing
    /// </summary>
    public class RealisticTestHandResolutionService : IHandResolutionService
    {
        public int GetCardStrength(Card card)
        {
            // Provide realistic card strengths for testing
            return card.Value switch
            {
                "4" => 1,   // Very weak
                "5" => 2,   // Very weak  
                "6" => 3,   // Very weak
                "7" => 4,   // Weak
                "Queen" => 5, // Weak
                "Jack" => 6,  // Weak
                "King" => 7,  // Medium
                "Ace" => 8,   // Medium
                "2" => 9,     // Medium-Strong
                "3" => 10,    // Strong
                "10" => 11,   // Strong
                _ => 5        // Default medium
            };
        }
        
        public Player? DetermineRoundWinner(List<PlayedCard> playedCards, List<Player> players) => 
            players.FirstOrDefault();
        
        public bool IsRoundDraw(List<PlayedCard> playedCards, List<Player> players) => false;
        
        public string? HandleDrawResolution(GameState game, int roundNumber) => null;
        
        public bool IsHandComplete(GameState game) => false;
        
        public TrucoMineiro.API.Domain.Models.Team? GetHandWinner(GameState game) => null;
        
        public Player? DetermineHandWinner(GameState game) => null;
        
        public void UpdateGameScores(GameState game, Player handWinner, int points) { }

        public Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs)
        {
            return Task.CompletedTask;
        }
    }
}
