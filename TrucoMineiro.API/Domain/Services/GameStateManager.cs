using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Manages game lifecycle, timeouts, and state transitions
    /// </summary>
    public class GameStateManager : IGameStateManager
    {
        private readonly IGameRepository _gameRepository;
        private readonly IScoreCalculationService _scoreCalculationService;
        private readonly ITrucoRulesEngine _trucoRulesEngine;
        private static readonly TimeSpan GameTimeout = TimeSpan.FromMinutes(30);

        public GameStateManager(
            IGameRepository gameRepository,
            IScoreCalculationService scoreCalculationService,
            ITrucoRulesEngine trucoRulesEngine)
        {
            _gameRepository = gameRepository;
            _scoreCalculationService = scoreCalculationService;
            _trucoRulesEngine = trucoRulesEngine;
        }        public async Task<GameState> CreateGameAsync(string? playerName = null)
        {            var game = new GameState
            {
                Id = Guid.NewGuid().ToString(),
                CreatedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow,
                CurrentRound = 1,
                RoundWinners = new List<int>(),
                IsCompleted = false,                WinningTeam = null,
                Players = CreatePlayers(playerName),
                Deck = new Deck(),
                PlayedCards = new List<PlayedCard>(),
                CurrentPlayerIndex = 0,
                Team1Score = 0,
                Team2Score = 0,
                ActionLog = new List<ActionLogEntry>(),
                CurrentStake = 1,
                PendingTrucoCall = false,
                TrucoCallerSeat = null,
                LastTrucoResponse = null,
                // Set initial dealer according to configuration
                // In Truco Mineiro, the dealer is known as the "Pé" (foot in Portuguese)
                DealerSeat = GameConfiguration.InitialDealerSeat,
                FirstPlayerSeat = GameConfiguration.GetFirstPlayerSeat(GameConfiguration.InitialDealerSeat)
            };

            // Initialize dealer and active player properly
            SetupInitialPlayers(game);

            // Deal initial cards
            DealCards(game);

            await _gameRepository.SaveGameAsync(game);
            return game;
        }

        public async Task<GameState?> GetActiveGameAsync(string gameId)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null || IsGameExpired(game) || IsGameCompleted(game))
            {
                return null;
            }

            // Update last activity
            game.LastActivity = DateTime.UtcNow;
            await _gameRepository.SaveGameAsync(game);
            
            return game;
        }

        public async Task<bool> SaveGameAsync(GameState game)
        {
            game.LastActivity = DateTime.UtcNow;
            return await _gameRepository.SaveGameAsync(game);
        }

        public async Task<bool> RemoveGameAsync(string gameId)
        {
            return await _gameRepository.DeleteGameAsync(gameId);
        }

        public async Task<int> CleanupExpiredGamesAsync()
        {
            var expiredIds = await GetExpiredGameIdsAsync();
            int cleanedCount = 0;
            
            foreach (var gameId in expiredIds)
            {
                if (await _gameRepository.DeleteGameAsync(gameId))
                {
                    cleanedCount++;
                }
            }
            
            return cleanedCount;
        }

        public async Task<int> CleanupCompletedGamesAsync()
        {
            var allGames = await _gameRepository.GetAllGamesAsync();
            int cleanedCount = 0;

            foreach (var game in allGames)
            {
                if (IsGameCompleted(game))
                {
                    if (await _gameRepository.DeleteGameAsync(game.Id))
                    {
                        cleanedCount++;
                    }
                }
            }

            return cleanedCount;
        }

        public bool IsGameExpired(GameState game)
        {
            return DateTime.UtcNow - game.LastActivity > GameTimeout;
        }        public bool IsGameCompleted(GameState game)
        {
            return game.IsCompleted || _scoreCalculationService.IsGameComplete(game);
        }

        public async Task<bool> UpdateLastActivityAsync(string gameId)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null) return false;

            game.LastActivity = DateTime.UtcNow;
            await _gameRepository.SaveGameAsync(game);
            return true;
        }        public async Task<bool> IsGameExpiredAsync(string gameId)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null) return true;

            return IsGameExpired(game);
        }public async Task<List<string>> GetExpiredGameIdsAsync()
        {
            var allGames = await _gameRepository.GetAllGamesAsync();
            var expiredIds = new List<string>();

            foreach (var game in allGames)
            {
                if (IsGameExpired(game))
                {
                    expiredIds.Add(game.Id);
                }
            }

            return expiredIds;
        }

        public async Task<bool> CompleteGameAsync(string gameId, int winningTeam)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null) return false;

            game.IsCompleted = true;
            game.WinningTeam = winningTeam;
            game.LastActivity = DateTime.UtcNow;

            await _gameRepository.SaveGameAsync(game);
            return true;
        }        public async Task<bool> StartNewHandAsync(string gameId)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null || game.IsCompleted) return false;

            // Reset hand state
            game.CurrentRound = 1;
            game.RoundWinners.Clear();
            game.PlayedCards.Clear();
            game.CurrentStake = 1;
            game.PendingTrucoCall = false;
            game.TrucoCallerSeat = null;
            game.LastTrucoResponse = null;
            game.LastActivity = DateTime.UtcNow;            // Rotate dealer to next seat (clockwise) - the dealer moves to the left at the end of each hand
            // In Truco Mineiro, the dealer is known as the "Pé" (foot in Portuguese)
            game.DealerSeat = GameConfiguration.GetNextDealerSeat(game.DealerSeat);
            game.FirstPlayerSeat = GameConfiguration.GetFirstPlayerSeat(game.DealerSeat);

            // Clear player hands and deal new cards
            foreach (var player in game.Players)
            {
                player.Hand.Clear();
                player.IsActive = false;
                player.IsDealer = false;
            }

            // Set new dealer and active player
            SetupInitialPlayers(game);
            DealCards(game);

            await _gameRepository.SaveGameAsync(game);
            return true;
        }

        public async Task<bool> AdvanceToNextRoundAsync(string gameId)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null || game.IsCompleted) return false;

            if (game.CurrentRound < 3)
            {
                game.CurrentRound++;
                game.PlayedCards.Clear(); // Clear played cards for the new round
                game.LastActivity = DateTime.UtcNow;

                await _gameRepository.SaveGameAsync(game);
                return true;
            }

            return false;
        }

        public async Task<bool> RecordRoundWinnerAsync(string gameId, int winningTeam)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null || game.IsCompleted) return false;

            game.RoundWinners.Add(winningTeam);
            game.LastActivity = DateTime.UtcNow;

            // Check if hand is complete (2 out of 3 rounds won)
            var team1Wins = game.RoundWinners.Count(w => w == 1);
            var team2Wins = game.RoundWinners.Count(w => w == 2);

            if (team1Wins >= 2 || team2Wins >= 2)
            {
                // Hand is complete, award points
                var handWinner = team1Wins >= 2 ? 1 : 2;
                await AwardHandPointsAsync(game, handWinner);                // Check if game is complete
                if (_scoreCalculationService.IsGameComplete(game))
                {
                    var gameWinner = game.Team1Score >= 12 ? 1 : 2;
                    await CompleteGameAsync(gameId, gameWinner);
                }
            }

            await _gameRepository.SaveGameAsync(game);
            return true;
        }        private Task AwardHandPointsAsync(GameState game, int winningTeam)
        {
            var points = _trucoRulesEngine.CalculateHandPoints(game);
            
            if (winningTeam == 1)
            {
                game.Team1Score += points;
            }
            else
            {
                game.Team2Score += points;
            }

            game.ActionLog.Add(new ActionLogEntry("hand-result")
            {
                Action = $"Team {winningTeam} wins the hand and scores {points} point(s)!"
            });

            return Task.CompletedTask;
        }

        private List<Player> CreatePlayers(string? playerName = null)
        {
            return new List<Player>
            {
                new Player { Id = "player1", Name = playerName ?? "Human", Seat = 0, IsAI = false, Hand = new List<Card>() },
                new Player { Id = "ai1", Name = "AI 1", Seat = 1, IsAI = true, Hand = new List<Card>() },
                new Player { Id = "ai2", Name = "AI 2", Seat = 2, IsAI = true, Hand = new List<Card>() },
                new Player { Id = "ai3", Name = "AI 3", Seat = 3, IsAI = true, Hand = new List<Card>() }
            };
        }

        private void DealCards(GameState game)
        {
            game.Deck.Shuffle();
            
            // Deal 3 cards to each player
            for (int cardIndex = 0; cardIndex < 3; cardIndex++)
            {
                for (int playerIndex = 0; playerIndex < 4; playerIndex++)
                {
                    var card = game.Deck.DealCard();
                    if (card != null)
                    {
                        game.Players[playerIndex].Hand.Add(card);
                    }
                }
            }
        }        /// <summary>
        /// Sets up initial dealer and active player according to Truco rules.
        /// In Truco Mineiro, the dealer is known as the "Pé" (foot in Portuguese).
        /// The first player is always the one sitting to the left of the dealer.
        /// </summary>
        /// <param name="game">The game state to initialize</param>
        private void SetupInitialPlayers(GameState game)
        {
            var dealerSeat = game.DealerSeat;
            
            // Ensure all players start as inactive
            foreach (var player in game.Players)
            {
                player.IsActive = false;
                player.IsDealer = false;
            }
            
            // Set the dealer (Pé)
            var dealer = game.Players.FirstOrDefault(p => p.Seat == dealerSeat);
            if (dealer != null)
            {
                dealer.IsDealer = true;
            }
            
            // Set the first active player (left of the dealer)
            var firstPlayerSeat = GameConfiguration.GetFirstPlayerSeat(dealerSeat);
            var firstPlayer = game.Players.FirstOrDefault(p => p.Seat == firstPlayerSeat);
            if (firstPlayer != null)
            {
                firstPlayer.IsActive = true;
                game.CurrentPlayerIndex = firstPlayerSeat;
            }
            
            // Update game state
            game.FirstPlayerSeat = firstPlayerSeat;
        }
    }
}
