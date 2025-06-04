using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for managing Truco game logic
    /// </summary>
    public class GameService
    {
        private readonly Dictionary<string, GameState> _games = new();

        /// <summary>
        /// Creates a new game with 4 players and deals cards
        /// </summary>
        /// <returns>The newly created game state</returns>
        public GameState CreateGame()
        {
            var gameState = new GameState();
            gameState.InitializeGame();
            _games[gameState.GameId] = gameState;
            return gameState;
        }

        /// <summary>
        /// Gets a game by ID
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>The game state if found, null otherwise</returns>
        public GameState? GetGame(string gameId)
        {
            return _games.TryGetValue(gameId, out var gameState) ? gameState : null;
        }

        /// <summary>
        /// Plays a card from a player's hand
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerId">The ID of the player making the move</param>
        /// <param name="cardIndex">The index of the card in the player's hand</param>
        /// <returns>True if the card was played successfully, false otherwise</returns>
        public bool PlayCard(string gameId, string playerId, int cardIndex)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
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
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerId == player.Id);
            if (playedCard != null)
            {
                playedCard.Card = card;
            }
            else
            {
                game.PlayedCards.Add(new PlayedCard(player.Id, card));
            }

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("card-played")
            {
                PlayerId = player.Id,
                Card = $"{card.Value} of {card.Suit}"
            });

            // Move to the next player's turn
            MoveToNextPlayer(game);

            // Check if all players have played a card in this round
            if (game.PlayedCards.All(pc => pc.Card != null))
            {
                DetermineRoundWinner(game);
            }

            return true;
        }

        /// <summary>
        /// Call Truco to raise the stakes
        /// </summary>
        public bool CallTruco(string gameId, string playerId)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return false;
            }

            // Check if raising is allowed
            if (!game.IsRaiseEnabled || game.Stakes >= 12)
            {
                return false;
            }

            // Calculate the new stakes
            int newStakes;
            if (!game.IsTrucoCalled)
            {
                // First Truco call
                newStakes = 3;
                game.IsTrucoCalled = true;
            }
            else
            {
                // Raise stakes: 3 -> 6 -> 9 -> 12
                switch (game.Stakes)
                {
                    case 3: newStakes = 6; break;  // Seis
                    case 6: newStakes = 9; break;  // Nove
                    case 9: newStakes = 12; break; // Doze
                    default: return false;
                }
            }

            game.Stakes = newStakes;

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerId = player.Id,
                Action = $"Raised stakes to {newStakes}"
            });

            return true;
        }

        /// <summary>
        /// Raises the stakes after a Truco call
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerId">The ID of the player raising the stakes</param>
        /// <returns>True if the stakes were raised successfully, false otherwise</returns>
        public bool RaiseStakes(string gameId, string playerId)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return false;
            }

            // Check if we can raise (Truco must have been called)
            if (!game.IsTrucoCalled || !game.IsRaiseEnabled || game.Stakes >= 12)
            {
                return false;
            }

            // Calculate the new stakes
            int newStakes;
            switch (game.Stakes)
            {
                case 3: newStakes = 6; break;  // Seis
                case 6: newStakes = 9; break;  // Nove
                case 9: newStakes = 12; break; // Doze
                default: return false;
            }

            game.Stakes = newStakes;

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerId = player.Id,
                Action = $"Raised stakes to {newStakes}"
            });

            return true;
        }

        /// <summary>
        /// Folds the hand in response to a Truco or other challenge
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerId">The ID of the player folding</param>
        /// <returns>True if the fold was successful, false otherwise</returns>
        public bool Fold(string gameId, string playerId)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
            {
                return false;
            }

            // Get opposing team
            string opposingTeam = player.Team == "Player's Team" ? "Opponent Team" : "Player's Team";

            // Award points to the opposing team
            game.TeamScores[opposingTeam] += Math.Max(1, game.Stakes);

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerId = player.Id,
                Action = $"Folded, {opposingTeam} gains {game.Stakes} points"
            });

            // Add a new hand result to the log
            game.ActionLog.Add(new ActionLogEntry("hand-result")
            {
                HandNumber = game.CurrentHand,
                Winner = opposingTeam,
                WinnerTeam = opposingTeam
            });

            // Reset for the next hand
            ResetForNewHand(game);
            
            return true;
        }

        /// <summary>
        /// Starts a new hand in the current game
        /// </summary>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <returns>True if the new hand was started successfully, false otherwise</returns>
        public bool StartNewHand(string gameId)
        {
            var game = GetGame(gameId);
            if (game == null)
            {
                return false;
            }

            // Reset the game for a new hand
            ResetForNewHand(game);

            // Add to the action log
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                Action = $"Started hand {game.CurrentHand}"
            });

            return true;
        }

        /// <summary>
        /// Resets the game state for a new hand
        /// </summary>
        /// <param name="game">The game state to reset</param>
        private void ResetForNewHand(GameState game)
        {
            // Increment hand number
            game.CurrentHand++;
            
            // Reset stakes and flags
            game.Stakes = 1;
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
                game.PlayedCards.Add(new PlayedCard(game.Players[i].Id));
            }
        }

        /// <summary>
        /// Move to the next player's turn
        /// </summary>
        private void MoveToNextPlayer(GameState game)
        {
            // Find the current active player
            var currentPlayerIndex = game.Players.FindIndex(p => p.IsActive);
            if (currentPlayerIndex < 0)
            {
                // If no active player, start with the first player after the dealer
                currentPlayerIndex = game.FirstPlayerSeat - 1;
            }

            // Set the current player to inactive
            game.Players[currentPlayerIndex].IsActive = false;

            // Find the next player
            var nextPlayerIndex = (currentPlayerIndex + 1) % GameState.MaxPlayers;

            // Set the next player to active
            game.Players[nextPlayerIndex].IsActive = true;
        }

        /// <summary>
        /// Determine the winner of the current round
        /// </summary>
        private void DetermineRoundWinner(GameState game)
        {
            // In a real implementation, this would use Truco card ranking rules
            // For now, we'll just use a simple value comparison for demonstration

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
                    var player = game.Players.FirstOrDefault(p => p.Id == playedCard.PlayerId);
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
