using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service for calculating scores and determining game completion
    /// </summary>
    public class ScoreCalculationService : IScoreCalculationService
    {

        public int CalculateHandPoints(GameState game)
        {
            return CalculateHandPoints(game.CurrentStake, game.Team1Score, game.Team2Score);
        }

        public void AwardPoints(GameState game, string team, int points)
        {
            if (team == "Team1" || team == "1")
            {
                game.Team1Score += points;
            }
            else if (team == "Team2" || team == "2")
            {
                game.Team2Score += points;
            }
        }

        public bool IsGameOver(GameState game)
        {
            return IsGameComplete(game.Team1Score, game.Team2Score);
        }

        public string? GetGameWinner(GameState game)
        {
            var winningTeam = GetWinningTeam(game.Team1Score, game.Team2Score);
            return winningTeam switch
            {
                1 => "Team1",
                2 => "Team2",
                _ => null
            };
        }

        public int GetTeamScore(GameState game, string team)
        {
            return team switch
            {
                "Team1" or "1" => game.Team1Score,
                "Team2" or "2" => game.Team2Score,
                _ => 0
            };
        }

        public void ResetScores(GameState game)
        {
            game.Team1Score = 0;
            game.Team2Score = 0;
        }

        /// <summary>
        /// Validates if score update is valid
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <param name="team">The team to update</param>
        /// <param name="points">The points to add</param>
        /// <returns>True if valid, false otherwise</returns>
        public bool IsScoreUpdateValid(GameState game, string team, int points)
        {
            if (points <= 0) return false;
            
            var currentScore = GetTeamScore(game, team);
            return currentScore + points <= 20; // Reasonable upper bound
        }

        /// <summary>
        /// Checks if the game is complete (a team reached 12 points)
        /// </summary>
        /// <param name="game">The current game state</param>
        /// <returns>True if game is complete, false otherwise</returns>
        public bool IsGameComplete(GameState game)
        {
            return IsGameOver(game);
        }        public int CalculateHandPoints(int currentStake, int team1Score, int team2Score)
        {
            // Check for last hand rule - when either team reaches victory threshold,
            // all subsequent hands are worth TrucoCall points regardless of stake
            if (team1Score >= TrucoConstants.Game.WinningScore - TrucoConstants.Stakes.Initial || 
                team2Score >= TrucoConstants.Game.WinningScore - TrucoConstants.Stakes.Initial)
            {
                return TrucoConstants.Stakes.TrucoCall;
            }

            // Normal scoring based on current stake
            return currentStake switch
            {
                1 => 1,      // Regular hand
                3 => 3,      // After Truco
                6 => 6,      // After Retruco (Seis)
                9 => 9,      // After Vale Quatro
                12 => 12,    // After Doze (maximum)
                _ => 1       // Default fallback
            };
        }        public bool IsGameComplete(int team1Score, int team2Score)
        {
            return team1Score >= TrucoConstants.Game.WinningScore || team2Score >= TrucoConstants.Game.WinningScore;
        }

        public int? GetWinningTeam(int team1Score, int team2Score)
        {
            if (team1Score >= TrucoConstants.Game.WinningScore && team2Score >= TrucoConstants.Game.WinningScore)
            {
                // Both teams reached winning score, winner is the one with higher score
                return team1Score > team2Score ? 1 : 2;
            }
            else if (team1Score >= TrucoConstants.Game.WinningScore)
            {
                return 1;
            }            else if (team2Score >= TrucoConstants.Game.WinningScore)
            {
                return 2;
            }
            
            return null; // Game not complete
        }

        public int CalculateTotalGamePoints(int team1Score, int team2Score)
        {
            return team1Score + team2Score;
        }

        public bool IsMaoDe10Active(int team1Score, int team2Score)
        {
            // Legacy method - replaced with dynamic last hand calculation
            var lastHandThreshold = TrucoConstants.Game.WinningScore - TrucoConstants.Stakes.Initial;
            return team1Score >= lastHandThreshold || team2Score >= lastHandThreshold;
        }

        public int GetPointsUntilWin(int teamScore)
        {
            return Math.Max(0, TrucoConstants.Game.WinningScore - teamScore);
        }
    }
}
