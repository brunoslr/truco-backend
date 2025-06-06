using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service responsible for managing game flow and turn sequence
    /// </summary>
    public class GameFlowService : IGameFlowService
    {
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IGameRepository _gameRepository;

        public GameFlowService(IAIPlayerService aiPlayerService, IGameRepository gameRepository)
        {
            _aiPlayerService = aiPlayerService;
            _gameRepository = gameRepository;
        }

        /// <summary>
        /// Executes a player's card play and manages the subsequent game flow
        /// </summary>
        public bool PlayCard(GameState game, int playerSeat, int cardIndex)
        {
            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null || !player.IsActive)
            {
                return false;
            }

            if (cardIndex < 0 || cardIndex >= player.Hand.Count)
            {
                return false;
            }

            // Play the card
            var card = player.PlayCard(cardIndex);

            // Find the player's played card slot
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
            if (playedCard != null)
            {
                playedCard.Card = card;
            }
            else
            {
                game.PlayedCards.Add(new PlayedCard(player.Seat, card));
            }

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("card-played")
            {
                PlayerSeat = player.Seat,
                Card = $"{card.Value} of {card.Suit}"
            });

            // Move to the next player's turn
            AdvanceToNextPlayer(game);

            // Check if all players have played a card in this round
            if (IsRoundComplete(game))
            {
                DetermineRoundWinner(game);
            }

            return true;
        }

        /// <summary>
        /// Executes AI player turns in sequence with appropriate delays
        /// </summary>
        public async Task ProcessAITurnsAsync(GameState game, int aiPlayDelayMs)
        {
            // Continue playing for AI players until it's a human player's turn or round is complete
            var maxIterations = 10; // Prevent infinite loops
            var iterations = 0;

            while (iterations < maxIterations)
            {
                var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                if (activePlayer == null) break;

                // Check if it's a human player (seat 0 typically) or if round is complete
                if (activePlayer.Seat == 0 || IsRoundComplete(game))
                {
                    break;
                }

                // Add a delay before AI plays
                await Task.Delay(aiPlayDelayMs);

                // AI player selects and plays a card
                if (activePlayer.Hand.Count > 0)
                {
                    var cardIndex = _aiPlayerService.SelectCardToPlay(activePlayer, game);
                    if (cardIndex >= 0 && cardIndex < activePlayer.Hand.Count)
                    {
                        PlayCard(game, activePlayer.Seat, cardIndex);
                    }
                }

                iterations++;
            }
        }

        /// <summary>
        /// Checks if a hand is complete and processes end-of-hand logic
        /// </summary>
        public async Task ProcessHandCompletionAsync(GameState game, int newHandDelayMs)
        {
            // Check if all players have no cards left (hand is complete)
            if (game.Players.All(p => p.Hand.Count == 0))
            {
                // Add delay before starting a new hand
                await Task.Delay(newHandDelayMs);

                // Reset for the next hand
                ResetForNewHand(game);
            }
        }

        /// <summary>
        /// Advances the turn to the next player
        /// </summary>
        public void AdvanceToNextPlayer(GameState game)
        {
            // Find the current active player
            var currentActivePlayer = game.Players.FirstOrDefault(p => p.IsActive);
            if (currentActivePlayer == null)
            {
                // If no active player, set the first player (left of dealer) as active
                var firstPlayer = game.Players.FirstOrDefault(p => p.Seat == game.FirstPlayerSeat);
                if (firstPlayer != null)
                {
                    firstPlayer.IsActive = true;
                    game.CurrentPlayerIndex = firstPlayer.Seat;
                }
                return;
            }

            // Set the current player to inactive
            currentActivePlayer.IsActive = false;

            // Find the next player (to the left, which is next seat in clockwise order)
            var nextPlayerSeat = (currentActivePlayer.Seat + 1) % GameConfiguration.MaxPlayers;
            var nextPlayer = game.Players.FirstOrDefault(p => p.Seat == nextPlayerSeat);

            // Set the next player to active
            if (nextPlayer != null)
            {
                nextPlayer.IsActive = true;
                game.CurrentPlayerIndex = nextPlayer.Seat;
            }
        }

        /// <summary>
        /// Checks if all players have played their cards for the current round
        /// </summary>
        public bool IsRoundComplete(GameState game)
        {
            return game.PlayedCards.All(pc => pc.Card != null);
        }

        /// <summary>
        /// Determine the winner of the current round
        /// </summary>
        private void DetermineRoundWinner(GameState game)
        {
            // In a real implementation, this would use Truco card ranking rules
            // For now, we'll use a simple value comparison for demonstration

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

        /// <summary>
        /// Resets the game state for a new hand
        /// </summary>
        private void ResetForNewHand(GameState game)
        {
            // Increment hand number
            game.CurrentHand++;

            // Reset stakes and flags
            game.Stakes = TrucoConstants.Stakes.Initial;
            game.IsTrucoCalled = false;
            game.IsRaiseEnabled = true;

            // Rotate the dealer
            int currentDealerIndex = game.Players.FindIndex(p => p.IsDealer);
            int newDealerIndex = (currentDealerIndex + 1) % game.Players.Count;

            foreach (var player in game.Players)
            {
                player.IsDealer = false;
                player.IsActive = false;
                player.Hand.Clear();
            }

            game.Players[newDealerIndex].IsDealer = true;

            // Set the first player (left of dealer)
            int firstPlayerIndex = (newDealerIndex + 1) % game.Players.Count;
            game.Players[firstPlayerIndex].IsActive = true;
            game.FirstPlayerSeat = firstPlayerIndex;

            // Deal new cards
            var deck = new Deck();
            deck.Shuffle();
            foreach (var player in game.Players)
            {
                for (int i = 0; i < 3; i++)
                {
                    player.Hand.Add(deck.DrawCard());
                }
            }

            // Reset played cards
            game.PlayedCards.Clear();
            for (int i = 0; i < game.Players.Count; i++)
            {
                game.PlayedCards.Add(new PlayedCard(game.Players[i].Seat));
            }
        }

        /// <summary>
        /// Starts a new hand by resetting the game state
        /// </summary>
        public void StartNewHand(GameState game)
        {
            ResetForNewHand(game);
        }
    }
}
