using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// AI behavior constants for decision making in Truco
    /// </summary>
    internal static class AIBehaviorConstants
    {
        // Base decision thresholds
        public const double BASE_ACCEPT_THRESHOLD = 0.3;
        public const double BASE_RAISE_THRESHOLD = 0.7;
        public const double HIGH_STAKES_RAISE_THRESHOLD = 0.85; // For stakes >= 8
        public const double BASE_TRUCO_CALL_THRESHOLD = 0.6;
        
        // Aggression modifiers
        public const double BEHIND_SCORE_AGGRESSION_BONUS = 0.1;
        public const double WON_FIRST_ROUND_AGGRESSION_BONUS = 0.3; // Doubled as requested
        
        // Bluffing parameters
        public const double BLUFF_BASE_CHANCE = 0.25; // 25% base chance to bluff
        public const double WEAK_HAND_THRESHOLD = 0.2; // Consider hand "weak" below this
        public const double BLUFF_RANDOM_FACTOR = 0.3; // Random variation in bluff decisions
        
        // Victory calculation
        public const int VICTORY_THRESHOLD = 12;
        
        // Probability modifiers
        public const double RAISE_PROBABILITY_AT_HIGH_STAKES = 0.2; // 20% at high stakes
        public const double RAISE_PROBABILITY_NORMAL = 0.3; // 30% normally
        
        // Randomness factors
        public const double THRESHOLD_RANDOM_VARIATION = 0.1; // ±10% variation in thresholds
        public const double AGGRESSION_RANDOM_FACTOR = 0.05; // ±5% variation in aggression
    }

    /// <summary>
    /// Game context for AI decision making
    /// </summary>
    internal class GameContext
    {
        public int PlayerTeam { get; set; }
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public int ScoreDifference { get; set; }
        public bool HasWonFirstRound { get; set; }
        public int CurrentHand { get; set; }
        public List<int> RoundWinners { get; set; } = new List<int>();
    }

    /// <summary>
    /// Helper class to analyze the current round for AI decision making
    /// </summary>
    internal class RoundAnalysis
    {
        public bool CanWin { get; set; }
        public bool PartnerHasStrongest { get; set; }
        public bool ShouldAttemptWin { get; set; }
        public int StrongestCardStrength { get; set; } = -1;
        public int OpponentsStillToPlay { get; set; }
    }

    /// <summary>
    /// AI player service for automatic card selection and decision making
    /// </summary>
    public class AIPlayerService : IAIPlayerService
    {
        private readonly IHandResolutionService _handResolutionService;
        private readonly Random _random;

        public AIPlayerService(IHandResolutionService handResolutionService)
        {
            _handResolutionService = handResolutionService;
            _random = new Random();
        }
        public int SelectCardToPlay(Player player, GameState game)
        {
            if (player.Hand.Count == 0)
                return -1; // No cards to play

            var currentRoundCards = GetCurrentRoundPlayedCards(game);
            var roundPosition = GetPlayerRoundPosition(player.Seat, currentRoundCards, game);
            var partnerSeat = GetPartnerSeat(player.Seat);

            // Analyze the current round situation
            var roundAnalysis = AnalyzeCurrentRound(currentRoundCards, player, game);

            return SelectCardBasedOnStrategy(player.Hand, roundAnalysis, roundPosition, partnerSeat);
        }

        /// <summary>
        /// Sophisticated card selection based on Truco strategy rules
        /// </summary>
        private int SelectCardBasedOnStrategy(List<Card> hand, RoundAnalysis analysis, int position, int partnerSeat)
        {
            // Rule: If no chance of winning, play the weakest card
            if (!analysis.CanWin)
            {
                return GetWeakestCardIndex(hand);
            }

            // Rule: If partner has the strongest card and opponents still need to play
            if (analysis.PartnerHasStrongest && analysis.OpponentsStillToPlay > 0)
            {
                // Play higher card only if opponents still need to play
                return GetCardToSupportPartner(hand, analysis.StrongestCardStrength);
            }

            // Rule: If partner has strongest card and no opponents left to play
            if (analysis.PartnerHasStrongest && analysis.OpponentsStillToPlay == 0)
            {
                // Play weakest card to save stronger cards
                return GetWeakestCardIndex(hand);
            }

            // Rule: If we can win and need to win
            if (analysis.CanWin && analysis.ShouldAttemptWin)
            {
                return GetSmallestWinningCardIndex(hand, analysis.StrongestCardStrength);
            }

            // Rule: If first to play, consider hand strength
            if (position == 1) // First to play
            {
                var handStrength = AnalyzeHandStrength(hand);
                if (handStrength > 0.7) // Strong hand
                {
                    return GetStrongestCardIndex(hand);
                }
                else // Weak hand - play conservatively
                {
                    return GetMediumCardIndex(hand);
                }
            }

            // Default: Play weakest card if unsure
            return GetWeakestCardIndex(hand);
        }

        /// <summary>
        /// Analyze the current round situation for strategic decisions
        /// </summary>
        private RoundAnalysis AnalyzeCurrentRound(List<Card> currentRoundCards, Player player, GameState game)
        {
            var analysis = new RoundAnalysis();
            var partnerSeat = GetPartnerSeat(player.Seat);
            var playedSeats = GetPlayedSeats(game);

            if (currentRoundCards.Count == 0)
            {
                // First to play
                analysis.CanWin = true;
                analysis.ShouldAttemptWin = true;
                analysis.OpponentsStillToPlay = 3;
                return analysis;
            }

            // Find strongest card and who played it
            int strongestStrength = -1;
            int strongestPlayerSeat = -1;

            foreach (var playedCard in game.PlayedCards.Where(pc => !pc.Card.IsEmpty))
            {
                var strength = _handResolutionService.GetCardStrength(playedCard.Card!);
                if (strength > strongestStrength)
                {
                    strongestStrength = strength;
                    strongestPlayerSeat = playedCard.PlayerSeat;
                }
            }

            analysis.StrongestCardStrength = strongestStrength;
            analysis.PartnerHasStrongest = (strongestPlayerSeat == partnerSeat);

            // Count opponents still to play
            var allSeats = new[] { 0, 1, 2, 3 };
            var opponentSeats = allSeats.Where(s => s != player.Seat && s != partnerSeat);
            analysis.OpponentsStillToPlay = opponentSeats.Count(s => !playedSeats.Contains(s));

            // Check if we can win with any card
            analysis.CanWin = player.Hand.Any(card =>
                _handResolutionService.GetCardStrength(card) > strongestStrength);

            // Determine if we should attempt to win
            analysis.ShouldAttemptWin = !analysis.PartnerHasStrongest || analysis.OpponentsStillToPlay == 0;

            return analysis;
        }

        /// <summary>
        /// Get the player's position in the current round (1 = first, 2 = second, etc.)
        /// </summary>
        private int GetPlayerRoundPosition(int playerSeat, List<Card> currentRoundCards, GameState game)
        {
            var playedCount = game.PlayedCards.Count(pc => !pc.Card.IsEmpty);
            return playedCount + 1; // Position is 1-based
        }

        /// <summary>
        /// Get the partner's seat (team mate)
        /// </summary>
        private int GetPartnerSeat(int playerSeat)
        {
            // Team 1: seats 0 and 2, Team 2: seats 1 and 3
            return playerSeat switch
            {
                0 => 2, // Player 0's partner is player 2
                1 => 3, // Player 1's partner is player 3
                2 => 0, // Player 2's partner is player 0
                3 => 1, // Player 3's partner is player 1
                _ => -1
            };
        }

        /// <summary>
        /// Get seats that have already played in current round
        /// </summary>
        private List<int> GetPlayedSeats(GameState game)
        {
            return game.PlayedCards
                .Where(pc => !pc.Card.IsEmpty)
                .Select(pc => pc.PlayerSeat)
                .ToList();
        }

        /// <summary>
        /// Get card that supports partner without being too strong
        /// </summary>
        private int GetCardToSupportPartner(List<Card> hand, int strongestStrength)
        {
            // Look for a card slightly stronger than current strongest, but not the absolute strongest
            var sortedCards = hand
                .Select((card, index) => new { Card = card, Index = index, Strength = _handResolutionService.GetCardStrength(card) })
                .OrderBy(x => x.Strength)
                .ToList();

            // Find a card that's stronger than current strongest but not our best card
            var winningCards = sortedCards.Where(x => x.Strength > strongestStrength).ToList();

            if (winningCards.Any())
            {
                // Use the weakest winning card (save stronger cards for later)
                return winningCards.First().Index;
            }

            // If no winning card, play weakest card
            return sortedCards.First().Index;
        }

        /// <summary>
        /// Get the smallest card that can win the round
        /// </summary>
        private int GetSmallestWinningCardIndex(List<Card> hand, int targetStrength)
        {
            int bestIndex = -1;
            int bestStrength = int.MaxValue;

            for (int i = 0; i < hand.Count; i++)
            {
                int cardStrength = _handResolutionService.GetCardStrength(hand[i]);
                if (cardStrength > targetStrength && cardStrength < bestStrength)
                {
                    bestStrength = cardStrength;
                    bestIndex = i;
                }
            }

            return bestIndex >= 0 ? bestIndex : GetWeakestCardIndex(hand);
        }

        /// <summary>
        /// Get a medium-strength card (not strongest, not weakest)
        /// </summary>
        private int GetMediumCardIndex(List<Card> hand)
        {
            if (hand.Count <= 2) return GetWeakestCardIndex(hand);

            var sortedCards = hand
                .Select((card, index) => new { Index = index, Strength = _handResolutionService.GetCardStrength(card) })
                .OrderBy(x => x.Strength)
                .ToList();

            // Return middle card if possible
            var middleIndex = sortedCards.Count / 2;
            return sortedCards[middleIndex].Index;
        }

        private int GetStrongestCardIndex(List<Card> hand)
        {
            if (hand.Count == 0) return -1;

            int bestIndex = 0;
            int bestStrength = _handResolutionService.GetCardStrength(hand[0]);

            for (int i = 1; i < hand.Count; i++)
            {
                int strength = _handResolutionService.GetCardStrength(hand[i]);
                if (strength > bestStrength)
                {
                    bestStrength = strength;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }

        private int GetWeakestCardIndex(List<Card> hand)
        {
            if (hand.Count == 0) return -1;

            int bestIndex = 0;
            int weakestStrength = _handResolutionService.GetCardStrength(hand[0]);

            for (int i = 1; i < hand.Count; i++)
            {
                int strength = _handResolutionService.GetCardStrength(hand[i]);
                if (strength < weakestStrength)
                {
                    weakestStrength = strength;
                    bestIndex = i;
                }
            }

            return bestIndex;
        }        public bool ShouldCallTruco(Player player, GameState game)
        {
            // Don't call if already at high stakes 
            if (game.Stakes >= TrucoConstants.Stakes.Seis)
                return false;
                
            // Don't call if there's already a pending truco call
            if (game.TrucoCallState != TrucoCallState.None)
                return false;

            var context = AnalyzeGameContext(player, game);
            var handStrength = AnalyzeHandStrength(player.Hand);

            // ABSOLUTE RULE: Never call truco when own victory is assured with current stakes
            if (context.TeamScore + game.Stakes >= AIBehaviorConstants.VICTORY_THRESHOLD)
                return false;

            // ABSOLUTE RULE: Always be aggressive when enemy is about to win
            if (context.OpponentScore + game.Stakes >= AIBehaviorConstants.VICTORY_THRESHOLD)
            {
                // Make desperate truco calls even with weak hands
                var desperationThreshold = context.HasWonFirstRound ? 0.8 : 0.6;
                return _random.NextDouble() < desperationThreshold;
            }

            // Calculate aggression bonus and apply randomness
            var aggressionBonus = CalculateAggressionBonus(context);
            var baseThreshold = ApplyRandomVariation(AIBehaviorConstants.BASE_TRUCO_CALL_THRESHOLD);
            var callThreshold = Math.Max(0.1, baseThreshold - aggressionBonus); // More aggressive = lower threshold

            // Consider bluffing opportunity
            if (ShouldAttemptBluff(context, handStrength))
            {
                // Bluff calls are more likely with strong position (won first round)
                return _random.NextDouble() < 0.7;
            }

            // Standard truco call decision with enhanced logic
            return handStrength >= callThreshold;
        }        public bool ShouldRaise(Player player, GameState game)
        {
            // Can't raise if at max stakes or no truco call pending
            if (game.Stakes >= TrucoConstants.Stakes.Maximum || game.TrucoCallState == TrucoCallState.None)
                return false;
                
            // Can't raise if at max truco level (Doze)
            if (game.TrucoCallState == TrucoCallState.Doze)
                return false;

            var context = AnalyzeGameContext(player, game);
            var handStrength = AnalyzeHandStrength(player.Hand);

            // ABSOLUTE RULE: Never raise when own victory is assured
            var nextStakeLevel = GetNextStakeLevel(game.Stakes);
            if (context.TeamScore + nextStakeLevel >= AIBehaviorConstants.VICTORY_THRESHOLD)
                return false;

            // ABSOLUTE RULE: Be aggressive when enemy is about to win
            if (context.OpponentScore + game.Stakes >= AIBehaviorConstants.VICTORY_THRESHOLD)
            {
                // Desperate raise attempts even with weak hands
                var desperationThreshold = context.HasWonFirstRound ? 0.7 : 0.5;
                return _random.NextDouble() < desperationThreshold;
            }

            // Calculate thresholds with aggression and randomness
            var aggressionBonus = CalculateAggressionBonus(context);
            var raiseThreshold = ApplyRandomVariation(
                game.Stakes >= 8 ? AIBehaviorConstants.HIGH_STAKES_RAISE_THRESHOLD : AIBehaviorConstants.BASE_RAISE_THRESHOLD
            );
            raiseThreshold = Math.Max(0.1, raiseThreshold - aggressionBonus); // More aggressive = lower threshold

            // Consider bluffing opportunity
            if (ShouldAttemptBluff(context, handStrength))
            {
                // Bluff raises are riskier, so lower probability
                return _random.NextDouble() < 0.5;
            }

            // Standard raise decision with probability factor
            if (handStrength >= raiseThreshold)
            {
                var raiseProb = game.Stakes >= 8 ? 
                    AIBehaviorConstants.RAISE_PROBABILITY_AT_HIGH_STAKES : 
                    AIBehaviorConstants.RAISE_PROBABILITY_NORMAL;
                
                return _random.NextDouble() < raiseProb;
            }

            return false;
        }public bool ShouldFold(Player player, GameState game)
        {
            // Only consider folding if there's a pending truco call to respond to
            if (game.TrucoCallState == TrucoCallState.None) return false;            var handStrength = AnalyzeHandStrength(player.Hand);
            var isPlayerTeam = GetPlayerTeam(player.Seat) == 1;
            var teamScore = isPlayerTeam ? game.Team1Score : game.Team2Score;

            // More likely to fold with weak hands or when ahead
            var foldProbability = (1 - handStrength) * 0.6;
            
            // More likely to fold when at last hand (close to victory)
            var lastHandThreshold = TrucoConstants.Game.WinningScore - TrucoConstants.Stakes.Initial;
            if (teamScore >= lastHandThreshold) foldProbability += 0.2;

            return _random.NextDouble() < foldProbability;
        }

        public bool IsAIPlayer(Player player)
        {
            return player.IsAI || player.Seat != 0; // Seat 0 is typically human player
        }
        private List<Card> GetCurrentRoundPlayedCards(GameState gameState)
        {
            // Get cards played in the current round only
            var currentRoundCards = gameState.PlayedCards
                .Where(pc => !pc.Card.IsFold)
                .Select(pc => pc.Card)
                .ToList();

            return currentRoundCards;
        }

        private double AnalyzeHandStrength(List<Card> hand)
        {
            if (hand.Count == 0) return 0;

            var totalStrength = hand.Sum(c => _handResolutionService.GetCardStrength(c));
            var maxPossibleStrength = hand.Count * 14; // Maximum card strength is 14 (Zap)

            return (double)totalStrength / maxPossibleStrength;
        }
        private int GetPlayerTeam(int seat)
        {
            // Team 0 (PlayerTeam): seats 0 and 2, Team 1 (OpponentTeam): seats 1 and 3
            return (seat % 2 == 0) ? 0 : 1;
        }        /// <summary>
        /// Determines how an AI player should respond to a truco call
        /// Enhanced with strategic decision making, bluffing, and victory awareness
        /// </summary>
        public TrucoDecision DecideTrucoResponse(Player player, GameState game, string callType, int newPotentialStakes)
        {
            var context = AnalyzeGameContext(player, game);
            var handStrength = AnalyzeHandStrength(player.Hand);
            var random = _random.NextDouble();

            // ABSOLUTE RULE 1: Fight when enemy will win - never surrender if opponent victory is imminent
            if (context.OpponentScore + game.Stakes >= AIBehaviorConstants.VICTORY_THRESHOLD)
            {
                // Try to raise if possible (even as a bluff), otherwise accept
                if (CanRaise(newPotentialStakes) && ShouldAttemptDesperateRaise(context, handStrength))
                {
                    return TrucoDecision.Raise;
                }
                return TrucoDecision.Accept; // Never surrender when enemy will win
            }

            // ABSOLUTE RULE 2: Conservative when close to victory - don't risk unnecessary points
            if (context.TeamScore + GetNextStakeLevel(newPotentialStakes) >= AIBehaviorConstants.VICTORY_THRESHOLD)
            {
                // Only accept or surrender, never raise when victory is assured
                var conservativeThreshold = ApplyRandomVariation(AIBehaviorConstants.BASE_ACCEPT_THRESHOLD);
                return handStrength >= conservativeThreshold ? TrucoDecision.Accept : TrucoDecision.Surrender;
            }

            // STRATEGIC DECISION MAKING with randomness
            var aggressionBonus = CalculateAggressionBonus(context);
            var acceptThreshold = ApplyRandomVariation(AIBehaviorConstants.BASE_ACCEPT_THRESHOLD + aggressionBonus);
            var raiseThreshold = ApplyRandomVariation(
                newPotentialStakes >= 8 ? AIBehaviorConstants.HIGH_STAKES_RAISE_THRESHOLD : AIBehaviorConstants.BASE_RAISE_THRESHOLD
            ) + aggressionBonus;

            // Consider bluffing opportunity
            if (ShouldAttemptBluff(context, handStrength))
            {
                return CanRaise(newPotentialStakes) ? TrucoDecision.Raise : TrucoDecision.Accept;
            }

            // Standard decision making with enhanced thresholds
            if (handStrength >= raiseThreshold && CanRaise(newPotentialStakes))
            {
                var raiseProb = newPotentialStakes >= 8 ? 
                    AIBehaviorConstants.RAISE_PROBABILITY_AT_HIGH_STAKES : 
                    AIBehaviorConstants.RAISE_PROBABILITY_NORMAL;
                
                if (random < raiseProb)
                {
                    return TrucoDecision.Raise;
                }
            }

            return handStrength >= acceptThreshold ? TrucoDecision.Accept : TrucoDecision.Surrender;
        }

        /// <summary>
        /// Analyzes the current game context for AI decision making
        /// </summary>
        private GameContext AnalyzeGameContext(Player player, GameState game)
        {
            var playerTeam = GetPlayerTeam(player.Seat);
            var teamScore = playerTeam == 0 ? game.TeamScores[Team.PlayerTeam] : game.TeamScores[Team.OpponentTeam];
            var opponentScore = playerTeam == 0 ? game.TeamScores[Team.OpponentTeam] : game.TeamScores[Team.PlayerTeam];

            return new GameContext
            {
                PlayerTeam = playerTeam,
                TeamScore = teamScore,
                OpponentScore = opponentScore,
                ScoreDifference = opponentScore - teamScore,
                HasWonFirstRound = HasWonFirstRound(playerTeam, game),
                CurrentHand = game.CurrentHand,
                RoundWinners = game.RoundWinners.ToList()
            };
        }        /// <summary>
        /// Checks if the AI's team won the first round of the current hand
        /// </summary>
        private bool HasWonFirstRound(int playerTeam, GameState game)
        {
            if (game.RoundWinners.Count == 0) return false;
            var firstRoundWinner = game.RoundWinners[0];
            
            // Convert playerTeam (0 or 1) to Team enum value (1 or 2)
            // playerTeam 0 = PlayerTeam (1), playerTeam 1 = OpponentTeam (2)
            var teamEnumValue = playerTeam == 0 ? (int)Team.PlayerTeam : (int)Team.OpponentTeam;
            
            return firstRoundWinner == teamEnumValue;
        }

        /// <summary>
        /// Calculates aggression bonus based on game context
        /// </summary>
        private double CalculateAggressionBonus(GameContext context)
        {
            double aggression = 0.0;
            
            // Behind in score bonus
            if (context.ScoreDifference > 0)
                aggression += AIBehaviorConstants.BEHIND_SCORE_AGGRESSION_BONUS;
            
            // Won first round bonus (doubled as requested)
            if (context.HasWonFirstRound)
                aggression += AIBehaviorConstants.WON_FIRST_ROUND_AGGRESSION_BONUS;
            
            // Add small random variation to aggression
            var randomFactor = (_random.NextDouble() - 0.5) * 2 * AIBehaviorConstants.AGGRESSION_RANDOM_FACTOR;
            aggression += randomFactor;
            
            return Math.Max(0, aggression); // Ensure non-negative
        }

        /// <summary>
        /// Applies random variation to threshold values for more realistic AI behavior
        /// </summary>
        private double ApplyRandomVariation(double threshold)
        {
            var variation = (_random.NextDouble() - 0.5) * 2 * AIBehaviorConstants.THRESHOLD_RANDOM_VARIATION;
            return Math.Max(0.05, Math.Min(0.95, threshold + variation)); // Keep within reasonable bounds
        }        /// <summary>
        /// Determines if the AI should attempt a bluff based on game context
        /// Enhanced with multiple bluffing scenarios for realistic behavior
        /// </summary>
        private bool ShouldAttemptBluff(GameContext context, double handStrength)
        {
            // Case A: Won first round but has weak cards - prime bluffing opportunity
            if (context.HasWonFirstRound && handStrength < AIBehaviorConstants.WEAK_HAND_THRESHOLD)
            {
                var bluffChance = AIBehaviorConstants.BLUFF_BASE_CHANCE + 0.15; // 40% total
                var randomFactor = _random.NextDouble() * AIBehaviorConstants.BLUFF_RANDOM_FACTOR;
                return _random.NextDouble() < (bluffChance + randomFactor);
            }
            
            // Case B: Behind in score with weak-to-medium hand - desperation bluff
            if (context.ScoreDifference > 0 && handStrength >= AIBehaviorConstants.WEAK_HAND_THRESHOLD && handStrength < 0.5)
            {
                var desperationBluffChance = AIBehaviorConstants.BLUFF_BASE_CHANCE;
                // More desperate if further behind
                if (context.ScoreDifference >= 3) desperationBluffChance += 0.1;
                return _random.NextDouble() < desperationBluffChance;
            }
            
            // Case C: Early hand with medium hand - positional bluff
            if (context.CurrentHand <= 2 && handStrength >= 0.4 && handStrength < 0.6)
            {
                var earlyGameBluffChance = AIBehaviorConstants.BLUFF_BASE_CHANCE * 0.6; // 15%
                return _random.NextDouble() < earlyGameBluffChance;
            }
            
            // Case D: Random bluff with any hand (pure unpredictability)
            var pureRandomBluff = AIBehaviorConstants.BLUFF_BASE_CHANCE * 0.3; // 7.5%
            return _random.NextDouble() < pureRandomBluff;
        }

        /// <summary>
        /// Checks if AI can raise given the current stakes
        /// </summary>
        private bool CanRaise(int currentStakes)
        {
            return currentStakes < AIBehaviorConstants.VICTORY_THRESHOLD;
        }

        /// <summary>
        /// Determines if AI should attempt a desperate raise when enemy is about to win
        /// </summary>
        private bool ShouldAttemptDesperateRaise(GameContext context, double handStrength)
        {
            // When enemy is about to win, raise even with weak hands (desperation bluff)
            // Higher chance if we won first round (gives false confidence)
            var desperationThreshold = context.HasWonFirstRound ? 0.7 : 0.5;
            return _random.NextDouble() < desperationThreshold;
        }

        /// <summary>
        /// Gets the next stake level if a raise were to happen
        /// </summary>
        private int GetNextStakeLevel(int currentStakes)
        {
            var progression = TrucoConstants.Stakes.Progression;
            for (int i = 0; i < progression.Length - 1; i++)
            {
                if (progression[i] == currentStakes)
                    return progression[i + 1];
            }
            return currentStakes; // Already at maximum
        }
    }
}
