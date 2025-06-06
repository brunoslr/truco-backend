using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service responsible for reacting to card play actions and managing subsequent game flow
    /// </summary>
    public class GameFlowReactionService : IGameFlowReactionService
    {
        private readonly IGameFlowService _gameFlowService;
        private readonly IAIPlayerService _aiPlayerService;

        public GameFlowReactionService(IGameFlowService gameFlowService, IAIPlayerService aiPlayerService)
        {
            _gameFlowService = gameFlowService;
            _aiPlayerService = aiPlayerService;
        }

        /// <summary>
        /// Processes all reactions after a card is played
        /// </summary>
        public async Task ProcessCardPlayReactionsAsync(GameState game, bool autoAiPlay, int aiPlayDelayMs, int newHandDelayMs)
        {
            // 1. Check if round is complete and determine winner
            ProcessRoundCompletion(game);

            // 2. Process hand completion if needed
            await ProcessHandCompletionAsync(game, newHandDelayMs);

            // 3. Process AI turns if enabled
            if (autoAiPlay)
            {
                await ProcessAITurnsAsync(game, aiPlayDelayMs);
            }
        }

        /// <summary>
        /// Checks if round is complete and determines winner
        /// </summary>
        public void ProcessRoundCompletion(GameState game)
        {
            if (_gameFlowService.IsRoundComplete(game))
            {
                // Determine round winner using existing logic from GameFlowService
                DetermineRoundWinner(game);
            }
        }

        /// <summary>
        /// Processes AI turns if enabled
        /// </summary>
        public async Task ProcessAITurnsAsync(GameState game, int aiPlayDelayMs)
        {
            await _gameFlowService.ProcessAITurnsAsync(game, aiPlayDelayMs);
        }

        /// <summary>
        /// Checks if hand is complete and starts new hand if needed
        /// </summary>
        public async Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs)
        {
            await _gameFlowService.ProcessHandCompletionAsync(game, newHandDelayMs);
        }

        /// <summary>
        /// Determine the winner of the current round (moved from GameFlowService)
        /// </summary>
        private void DetermineRoundWinner(GameState game)
        {
            // Define card strength (Truco Mineiro uses different card strengths than traditional decks)
            var cardStrength = new Dictionary<string, int>
            {
                { "4", 1 }, { "5", 2 }, { "6", 3 }, { "7", 4 },
                { "Q", 5 }, { "J", 6 }, { "K", 7 }, { "A", 8 },
                { "2", 9 }, { "3", 10 }
            };

            var strongestCard = -1;
            Player? roundWinner = null;

            foreach (var playedCard in game.PlayedCards)
            {
                if (playedCard.Card != null)
                {
                    var player = game.Players.FirstOrDefault(p => p.Seat == playedCard.PlayerSeat);
                    if (player != null)
                    {
                        var cardValue = cardStrength.GetValueOrDefault(playedCard.Card.Value, 0);
                        if (cardValue > strongestCard)
                        {
                            strongestCard = cardValue;
                            roundWinner = player;
                        }
                    }
                }
            }

            if (roundWinner != null)
            {
                // Set the winner and add to log
                game.TurnWinner = roundWinner.Team;
                game.ActionLog.Add(new ActionLogEntry("turn-result")
                {
                    Winner = roundWinner.Name,
                    WinnerTeam = roundWinner.Team
                });

                // Add the points to the winner's team
                game.AddScore(roundWinner.Team, game.Stakes);

                // Clear the played cards for the next round or hand
                if (game.PlayedCards.Any(pc => pc.Card != null && game.Players.Any(p => p.Hand.Count == 0)))
                {
                    // If any player has no cards left, move to the next hand
                    game.NextHand();
                }
                else
                {
                    // Otherwise, clear for the next round in the same hand
                    foreach (var pc in game.PlayedCards)
                    {
                        pc.Card = null;
                    }
                }
            }
        }
    }
}
