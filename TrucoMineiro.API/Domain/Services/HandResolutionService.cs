using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{    /// <summary>
    /// Implementation of hand resolution logic for Truco Mineiro
    /// </summary>
    public class HandResolutionService : IHandResolutionService
    {
        private readonly IEventPublisher _eventPublisher;

        public HandResolutionService(IEventPublisher eventPublisher)
        {
            _eventPublisher = eventPublisher;
        }
        
        // Truco Mineiro card hierarchy (higher values = stronger cards)
        private readonly Dictionary<string, Dictionary<string, int>> _cardStrengths = new()
        {
            // Special cards (manilhas)
            ["4"] = new Dictionary<string, int> { [SuitConstants.Clubs] = 14 }, // Zap (highest)
            ["7"] = new Dictionary<string, int> 
            { 
                [SuitConstants.Hearts] = 13,    // Copas (second highest)
                [SuitConstants.Diamonds] = 11,  // Espadinha (fourth highest)
                [SuitConstants.Spades] = 4,     // Regular 7
                [SuitConstants.Clubs] = 4       // Regular 7
            },
            ["A"] = new Dictionary<string, int> 
            { 
                [SuitConstants.Spades] = 12,    // Espadão (third highest)
                [SuitConstants.Hearts] = 8,     // Regular Ace
                [SuitConstants.Diamonds] = 8,   // Regular Ace
                [SuitConstants.Clubs] = 8       // Regular Ace
            },
            
            // Regular cards in order
            ["3"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 10, [SuitConstants.Diamonds] = 10, [SuitConstants.Spades] = 10, [SuitConstants.Clubs] = 10 },
            ["2"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 9, [SuitConstants.Diamonds] = 9, [SuitConstants.Spades] = 9, [SuitConstants.Clubs] = 9 },
            ["K"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 7, [SuitConstants.Diamonds] = 7, [SuitConstants.Spades] = 7, [SuitConstants.Clubs] = 7 },
            ["J"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 6, [SuitConstants.Diamonds] = 6, [SuitConstants.Spades] = 6, [SuitConstants.Clubs] = 6 },
            ["Q"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 5, [SuitConstants.Diamonds] = 5, [SuitConstants.Spades] = 5, [SuitConstants.Clubs] = 5 },
            ["6"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 3, [SuitConstants.Diamonds] = 3, [SuitConstants.Spades] = 3, [SuitConstants.Clubs] = 3 },
            ["5"] = new Dictionary<string, int> { [SuitConstants.Hearts] = 2, [SuitConstants.Diamonds] = 2, [SuitConstants.Spades] = 2, [SuitConstants.Clubs] = 2 }
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
        
        public bool IsRoundDraw(List<PlayedCard> playedCards, List<Player> players)
        {
            return DetermineRoundWinner(playedCards, players) == null && 
                   playedCards.Any(pc => !pc.Card.IsEmpty);
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
        }
        public bool IsHandComplete(GameState game)
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
        }

        public string? GetHandWinner(GameState game)
        {
            if (!IsHandComplete(game))
                return null;

            var teamWins = new Dictionary<int, int>();

            foreach (var winningTeam in game.RoundWinners)
            {
                teamWins[winningTeam] = teamWins.GetValueOrDefault(winningTeam, 0) + 1;
            }            var winningTeamNumber = teamWins.FirstOrDefault(kvp => kvp.Value >= 2).Key;
            return winningTeamNumber == 0 ? null : $"Team{winningTeamNumber}";
        }

        public async Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs)
        {
            // Use the proper hand resolution service to check completion
            if (IsHandComplete(game))
            {
                // Determine the winning team and round information
                var winningTeam = game.TurnWinner ?? TrucoConstants.Teams.PlayerTeam;
                var roundWinners = new List<int>(); // This would need to be tracked throughout the hand
                var pointsAwarded = game.Stakes;

                // Publish hand completed event which will trigger cleanup
                await _eventPublisher.PublishAsync(new HandCompletedEvent(
                    Guid.Parse(game.Id), 
                    game.CurrentHand, 
                    winningTeam,
                    roundWinners,
                    pointsAwarded,
                    game));

                // Add delay before starting a new hand
                await Task.Delay(newHandDelayMs);

                // Determine new dealer and first player for next hand
                var currentDealerSeat = game.Players.FindIndex(p => p.IsDealer);
                var newDealerSeat = (currentDealerSeat + 1) % TrucoConstants.Game.MaxPlayers;
                var newFirstPlayerSeat = (newDealerSeat + 1) % TrucoConstants.Game.MaxPlayers;

                // Publish hand started event for the new hand
                await _eventPublisher.PublishAsync(new HandStartedEvent(
                    Guid.Parse(game.Id), 
                    game.CurrentHand + 1,
                    newDealerSeat,
                    newFirstPlayerSeat,
                    game));
            }
        }
    }
}
