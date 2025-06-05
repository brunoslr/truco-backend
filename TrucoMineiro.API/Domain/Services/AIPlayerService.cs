using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Domain.Services
{
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
        }        public int SelectCardToPlay(Player player, GameState game)
        {
            if (player.Hand.Count == 0)
                return -1; // No cards to play

            // Simple AI: analyze hand strength and game situation
            var handStrength = AnalyzeHandStrength(player.Hand);
            var currentRoundCards = GetCurrentRoundPlayedCards(game);
            
            // Strategy based on game situation
            if (handStrength > 0.7)
            {
                // Strong hand - play strongest card
                return GetStrongestCardIndex(player.Hand);
            }
            else if (currentRoundCards.Count == 3)
            {
                // Last to play - try to win with weakest winning card or play weakest
                return GetBestLastCardIndex(player.Hand, currentRoundCards);
            }
            else
            {
                // Conservative play - play weakest card
                return GetWeakestCardIndex(player.Hand);
            }
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
        }

        private int GetBestLastCardIndex(List<Card> hand, List<Card> currentRoundCards)
        {
            if (hand.Count == 0) return -1;
            if (currentRoundCards.Count == 0) return GetWeakestCardIndex(hand);
            
            // Find the strongest card played by opponents
            int strongestOpponentStrength = currentRoundCards.Max(c => _handResolutionService.GetCardStrength(c));
            
            // Try to find the weakest card that can still win
            int bestIndex = -1;
            int bestStrength = int.MaxValue;
            
            for (int i = 0; i < hand.Count; i++)
            {
                int cardStrength = _handResolutionService.GetCardStrength(hand[i]);
                if (cardStrength > strongestOpponentStrength && cardStrength < bestStrength)
                {
                    bestStrength = cardStrength;
                    bestIndex = i;
                }
            }
            
            // If no card can win, play the weakest card
            return bestIndex >= 0 ? bestIndex : GetWeakestCardIndex(hand);
        }

        public bool ShouldCallTruco(Player player, GameState game)
        {
            // Don't call if already at high stakes or pending call exists
            if (game.CurrentStake >= 6 || game.PendingTrucoCall)
                return false;

            var handStrength = AnalyzeHandStrength(player.Hand);
            var isPlayerTeam = GetPlayerTeam(player.Seat) == 1;
            var teamScore = isPlayerTeam ? game.Team1Score : game.Team2Score;
            var opponentScore = isPlayerTeam ? game.Team2Score : game.Team1Score;

            // More likely to call with strong hands or when behind
            var callProbability = handStrength * 0.6;
            if (opponentScore > teamScore) callProbability += 0.2;

            return _random.NextDouble() < callProbability;
        }

        public bool ShouldRaise(Player player, GameState game)
        {
            if (game.CurrentStake >= 12 || !game.PendingTrucoCall)
                return false;

            var handStrength = AnalyzeHandStrength(player.Hand);
            return handStrength > 0.8 && _random.NextDouble() < 0.3;
        }

        public bool ShouldFold(Player player, GameState game)
        {
            if (!game.PendingTrucoCall) return false;

            var handStrength = AnalyzeHandStrength(player.Hand);
            var isPlayerTeam = GetPlayerTeam(player.Seat) == 1;
            var teamScore = isPlayerTeam ? game.Team1Score : game.Team2Score;

            // More likely to fold with weak hands or when ahead
            var foldProbability = (1 - handStrength) * 0.6;
            if (teamScore >= 10) foldProbability += 0.2;

            return _random.NextDouble() < foldProbability;
        }

        public bool ProcessAITurn(Player player, GameState game)
        {
            if (!IsAIPlayer(player) || !player.IsActive)
                return false;

            // Handle Truco response first if needed
            if (game.PendingTrucoCall && game.TrucoCallerSeat != player.Seat)
            {
                if (ShouldFold(player, game))
                {
                    // Handle fold logic here
                    return true;
                }
                else if (ShouldRaise(player, game))
                {
                    // Handle raise logic here
                    return true;
                }
                else
                {
                    // Accept the Truco
                    game.PendingTrucoCall = false;
                    return true;
                }
            }

            // Normal card play
            var cardIndex = SelectCardToPlay(player, game);
            if (cardIndex >= 0 && cardIndex < player.Hand.Count)
            {
                // This would integrate with the main game service to play the card
                return true;
            }

            return false;
        }

        public int ProcessAllAITurns(GameState game)
        {
            int processedTurns = 0;
            int maxIterations = 10; // Prevent infinite loops

            for (int i = 0; i < maxIterations; i++)
            {
                var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                if (activePlayer == null || !IsAIPlayer(activePlayer))
                    break;

                if (ProcessAITurn(activePlayer, game))
                {
                    processedTurns++;
                }
                else
                {
                    break;
                }
            }

            return processedTurns;
        }

        public bool IsAIPlayer(Player player)
        {
            return player.IsAI || player.Seat != 0; // Seat 0 is typically human player
        }        private List<Card> GetCurrentRoundPlayedCards(GameState gameState)
        {
            // Get cards played in the current round only
            var currentRoundCards = gameState.PlayedCards
                .Where(pc => pc.Card != null)
                .Select(pc => pc.Card!)
                .ToList();
            
            return currentRoundCards;
        }

        private AIStrategy DetermineStrategy(GameState gameState, Player aiPlayer, List<Card> currentRoundCards)
        {
            var handStrength = AnalyzeHandStrength(aiPlayer.Hand);
            var isFirstToPlay = currentRoundCards.Count == 0;
            var canWinRound = CanWinCurrentRound(aiPlayer.Hand, currentRoundCards);
            var teamWinningRounds = CountTeamRoundWins(gameState, GetPlayerTeam(aiPlayer.Seat));

            // Strategy logic based on game situation
            if (handStrength > 0.8)
            {
                return AIStrategy.PlayStrongest; // Very strong hand, be aggressive
            }
            else if (teamWinningRounds >= 1 && gameState.CurrentRound >= 2)
            {
                return AIStrategy.Save; // Already won a round, play conservatively
            }
            else if (canWinRound && handStrength > 0.4)
            {
                return AIStrategy.TryToWin; // Can win this round, go for it
            }
            else if (isFirstToPlay && handStrength < 0.3)
            {
                return AIStrategy.PlayWeakest; // Weak hand and first to play, play low
            }
            else
            {
                return AIStrategy.Save; // Default to conservative play
            }
        }

        private Card SelectStrongestCard(List<Card> hand)
        {
            return hand.OrderByDescending(c => _handResolutionService.GetCardStrength(c)).First();
        }

        private Card SelectWeakestCard(List<Card> hand)
        {
            return hand.OrderBy(c => _handResolutionService.GetCardStrength(c)).First();
        }

        private Card TryToWinRound(List<Card> hand, List<Card> currentRoundCards)
        {
            if (currentRoundCards.Count == 0)
            {
                // First to play, play a medium-strength card
                var sortedHand = hand.OrderBy(c => _handResolutionService.GetCardStrength(c)).ToList();
                return sortedHand[sortedHand.Count / 2];
            }

            // Try to play the weakest card that can still win
            var strongestOpponentCard = currentRoundCards.Max(c => _handResolutionService.GetCardStrength(c));
            var winningCards = hand.Where(c => _handResolutionService.GetCardStrength(c) > strongestOpponentCard).ToList();
            
            if (winningCards.Any())
            {
                return winningCards.OrderBy(c => _handResolutionService.GetCardStrength(c)).First();
            }
            
            // Can't win, play the weakest card
            return SelectWeakestCard(hand);
        }

        private Card SaveStrongCards(List<Card> hand, List<Card> currentRoundCards)
        {
            // Play the weakest card that doesn't give away strong cards unnecessarily
            var sortedHand = hand.OrderBy(c => _handResolutionService.GetCardStrength(c)).ToList();
            
            // If we're last to play and can't win, definitely play weakest
            if (currentRoundCards.Count == 3)
            {
                var strongestOpponentCard = currentRoundCards.Max(c => _handResolutionService.GetCardStrength(c));
                var canWin = hand.Any(c => _handResolutionService.GetCardStrength(c) > strongestOpponentCard);
                
                if (!canWin)
                {
                    return sortedHand.First();
                }
            }
            
            // Conservative play - don't waste strong cards early
            return sortedHand.First();
        }

        private Card SelectRandomCard(List<Card> hand)
        {
            return hand[_random.Next(hand.Count)];
        }

        private double AnalyzeHandStrength(List<Card> hand)
        {
            if (hand.Count == 0) return 0;

            var totalStrength = hand.Sum(c => _handResolutionService.GetCardStrength(c));
            var maxPossibleStrength = hand.Count * 14; // Maximum card strength is 14 (Zap)
            
            return (double)totalStrength / maxPossibleStrength;
        }

        private double CalculateGameProgress(GameState gameState)
        {
            var myTeamScore = gameState.Team1Score; // Assume AI is always on team 1 for simplicity
            var opponentScore = gameState.Team2Score;
            
            // Higher value when behind (more likely to take risks)
            if (opponentScore > myTeamScore)
            {
                return 0.7;
            }
            else if (myTeamScore > opponentScore)
            {
                return 0.3;
            }
            else
            {
                return 0.5;
            }
        }

        private double CalculateRiskTolerance(GameState gameState, Player aiPlayer)
        {
            var teamScore = GetTeamScore(gameState, GetPlayerTeam(aiPlayer.Seat));
            var opponentScore = GetOpponentScore(gameState, GetPlayerTeam(aiPlayer.Seat));
            
            // Higher risk tolerance when behind
            if (opponentScore >= 10 && teamScore < 10)
            {
                return 0.8; // Desperate situation, high risk tolerance
            }
            else if (teamScore >= 10)
            {
                return 0.2; // Close to winning, low risk tolerance
            }
            else
            {
                return 0.5; // Normal situation
            }
        }

        private double CalculateRiskReward(GameState gameState, int currentStake)
        {
            var potentialGain = currentStake * 2; // Points we could gain
            var potentialLoss = currentStake; // Points we could lose
            
            // Simple risk/reward calculation
            return (double)potentialGain / (potentialGain + potentialLoss);
        }

        private bool CanWinCurrentRound(List<Card> hand, List<Card> currentRoundCards)
        {
            if (currentRoundCards.Count == 0) return true; // First to play, always "can win"
            
            var strongestOpponentCard = currentRoundCards.Max(c => _handResolutionService.GetCardStrength(c));
            return hand.Any(c => _handResolutionService.GetCardStrength(c) > strongestOpponentCard);
        }

        private int CountTeamRoundWins(GameState gameState, int team)
        {
            return gameState.RoundWinners.Count(w => w == team);
        }

        private int GetPlayerTeam(int seat)
        {
            // Team 1: seats 0 and 2, Team 2: seats 1 and 3
            return (seat % 2 == 0) ? 1 : 2;
        }

        private int GetTeamScore(GameState gameState, int team)
        {
            return team == 1 ? gameState.Team1Score : gameState.Team2Score;
        }

        private int GetOpponentScore(GameState gameState, int team)
        {
            return team == 1 ? gameState.Team2Score : gameState.Team1Score;
        }
    }

    public enum AIStrategy
    {
        PlayStrongest,
        PlayWeakest,
        TryToWin,
        Save,
        Random
    }
}
