using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
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
            // TODO: Inject TrucoRulesEngine for proper dynamic rule checking
            // Don't call if already at high stakes or if both teams are at last hand
            if (game.Stakes >= TrucoConstants.Stakes.Seis)
                return false;
                
            // Don't call if there's already a pending truco call
            if (game.TrucoCallState != TrucoCallState.None)
                return false;

            var handStrength = AnalyzeHandStrength(player.Hand);
            var isPlayerTeam = GetPlayerTeam(player.Seat) == 1;
            var teamScore = isPlayerTeam ? game.Team1Score : game.Team2Score;
            var opponentScore = isPlayerTeam ? game.Team2Score : game.Team1Score;

            // More likely to call with strong hands or when behind
            var callProbability = handStrength * 0.6;
            if (opponentScore > teamScore) callProbability += 0.2;

            return _random.NextDouble() < callProbability;
        }        public bool ShouldRaise(Player player, GameState game)
        {
            // Can't raise if at max stakes or no truco call pending
            if (game.Stakes >= TrucoConstants.Stakes.Maximum || game.TrucoCallState == TrucoCallState.None)
                return false;
                
            // Can't raise if at max truco level (Doze)
            if (game.TrucoCallState == TrucoCallState.Doze)
                return false;

            var handStrength = AnalyzeHandStrength(player.Hand);
            return handStrength > 0.8 && _random.NextDouble() < 0.3;
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
            // Team 1: seats 0 and 2, Team 2: seats 1 and 3
            return (seat % 2 == 0) ? 1 : 2;
        }
    }
}
