using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Implementation of hand resolution logic for Truco Mineiro
    /// </summary>
    public class HandResolutionService : IHandResolutionService
    {
        // Truco Mineiro card hierarchy (higher values = stronger cards)
        private readonly Dictionary<string, Dictionary<string, int>> _cardStrengths = new()
        {
            // Special cards (manilhas)
            ["4"] = new Dictionary<string, int> { ["Clubs"] = 14 }, // Zap (highest)
            ["7"] = new Dictionary<string, int> 
            { 
                ["Hearts"] = 13,    // Copas (second highest)
                ["Diamonds"] = 11,  // Espadinha (fourth highest)
                ["Spades"] = 4,     // Regular 7
                ["Clubs"] = 4       // Regular 7
            },
            ["A"] = new Dictionary<string, int> 
            { 
                ["Spades"] = 12,    // Espadão (third highest)
                ["Hearts"] = 8,     // Regular Ace
                ["Diamonds"] = 8,   // Regular Ace
                ["Clubs"] = 8       // Regular Ace
            },
            
            // Regular cards in order
            ["3"] = new Dictionary<string, int> { ["Hearts"] = 10, ["Diamonds"] = 10, ["Spades"] = 10, ["Clubs"] = 10 },
            ["2"] = new Dictionary<string, int> { ["Hearts"] = 9, ["Diamonds"] = 9, ["Spades"] = 9, ["Clubs"] = 9 },
            ["K"] = new Dictionary<string, int> { ["Hearts"] = 7, ["Diamonds"] = 7, ["Spades"] = 7, ["Clubs"] = 7 },
            ["J"] = new Dictionary<string, int> { ["Hearts"] = 6, ["Diamonds"] = 6, ["Spades"] = 6, ["Clubs"] = 6 },
            ["Q"] = new Dictionary<string, int> { ["Hearts"] = 5, ["Diamonds"] = 5, ["Spades"] = 5, ["Clubs"] = 5 },
            ["6"] = new Dictionary<string, int> { ["Hearts"] = 3, ["Diamonds"] = 3, ["Spades"] = 3, ["Clubs"] = 3 },
            ["5"] = new Dictionary<string, int> { ["Hearts"] = 2, ["Diamonds"] = 2, ["Spades"] = 2, ["Clubs"] = 2 }
        };

        public int GetCardStrength(Card card)
        {
            if (_cardStrengths.TryGetValue(card.Value, out var suits))
            {
                if (suits.TryGetValue(card.Suit, out var strength))
                {
                    return strength;
                }
            }
            
            // Default strength for cards not in hierarchy (shouldn't happen with proper deck)
            return 0;
        }

        public Player? DetermineRoundWinner(List<PlayedCard> playedCards, List<Player> players)
        {
            if (!playedCards.Any(pc => pc.Card != null))
                return null;

            Player? winner = null;
            int highestStrength = -1;
            bool hasDraw = false;            foreach (var playedCard in playedCards.Where(pc => pc.Card != null))
            {
                var player = players.FirstOrDefault(p => p.Seat == playedCard.PlayerSeat);
                if (player != null && playedCard.Card != null)
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

        public bool IsRoundDraw(List<PlayedCard> playedCards, List<Player> players)
        {
            return DetermineRoundWinner(playedCards, players) == null && 
                   playedCards.Any(pc => pc.Card != null);
        }

        public string? HandleDrawResolution(GameState game, int roundNumber)
        {
            // Truco Mineiro draw rules:
            // 1st round draw: Show highest card or players can choose to play/fold
            // 2nd/3rd round draw: Team that won first round wins the hand
            
            if (roundNumber == 1)
            {
                // First round draw - for now, we'll implement a simple resolution
                // In a full implementation, this would involve showing highest cards
                // For simplicity, we'll return null to indicate unresolved draw
                return null;
            }            else
            {
                // Second or third round draw - first round winner takes it
                if (game.RoundWinners.Count > 0)
                {
                    return game.RoundWinners[0].ToString();
                }
                return null;
            }
        }        public bool IsHandComplete(GameState game)
        {
            // Hand is complete when a team wins 2 out of 3 rounds
            var teamWins = new Dictionary<int, int>();
            
            foreach (var winningTeam in game.RoundWinners)
            {
                teamWins[winningTeam] = teamWins.GetValueOrDefault(winningTeam, 0) + 1;
                if (teamWins[winningTeam] >= 2)
                {
                    return true;
                }
            }
            
            return false;
        }        public string? GetHandWinner(GameState game)
        {
            if (!IsHandComplete(game))
                return null;

            var teamWins = new Dictionary<int, int>();
            
            foreach (var winningTeam in game.RoundWinners)
            {
                teamWins[winningTeam] = teamWins.GetValueOrDefault(winningTeam, 0) + 1;
            }
            
            var winningTeamNumber = teamWins.FirstOrDefault(kvp => kvp.Value >= 2).Key;
            return winningTeamNumber == 0 ? null : $"Team{winningTeamNumber}";
        }

        private string? GetPlayerTeam(GameState game, string playerName)
        {
            var player = game.Players.FirstOrDefault(p => p.Name == playerName);
            return player?.Team;
        }
    }
}
