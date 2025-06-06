using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service for calculating scores and determining game completion
    /// </summary>
    public class ScoreCalculationService : IScoreCalculationService
    {
        private const int WinningScore = 12;
        private const int MaoDe10Threshold = 10;

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
        }

        public int CalculateHandPoints(int currentStake, int team1Score, int team2Score)
        {
            // Check for "Mão de 10" rule - when either team reaches 10 points,
            // all subsequent hands are worth 4 points regardless of stake
            if (team1Score >= MaoDe10Threshold || team2Score >= MaoDe10Threshold)
            {
                return 4;
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
        }

        public bool IsGameComplete(int team1Score, int team2Score)
        {
            return team1Score >= WinningScore || team2Score >= WinningScore;
        }

        public int? GetWinningTeam(int team1Score, int team2Score)
        {
            if (team1Score >= WinningScore && team2Score >= WinningScore)
            {
                // Both teams reached 12+ points, winner is the one with higher score
                return team1Score > team2Score ? 1 : 2;
            }
            else if (team1Score >= WinningScore)
            {
                return 1;
            }
            else if (team2Score >= WinningScore)
            {
                return 2;
            }
            
            return null; // Game not complete
        }

        public int CalculateTotalGamePoints(int team1Score, int team2Score)
        {
            return team1Score + team2Score;
        }

        public double CalculateWinPercentage(int teamScore, int opponentScore)
        {
            if (teamScore >= WinningScore)
                return 100.0;
            
            if (opponentScore >= WinningScore)
                return 0.0;

            // Simple calculation based on score difference and proximity to winning
            var scoreAdvantage = teamScore - opponentScore;
            var progressToWin = (double)teamScore / WinningScore;
            var opponentProgress = (double)opponentScore / WinningScore;
            
            // Base percentage on current score
            var basePercentage = progressToWin * 50; // 0-50% based on progress
            
            // Adjust based on score difference
            var advantageAdjustment = scoreAdvantage * 5; // +/- 5% per point difference
            
            // Penalty if opponent is close to winning
            var opponentThreatPenalty = opponentProgress > 0.8 ? (opponentProgress - 0.8) * 50 : 0;
            
            var finalPercentage = basePercentage + advantageAdjustment - opponentThreatPenalty;
            
            // Clamp between 5% and 95%
            return Math.Max(5, Math.Min(95, finalPercentage));
        }

        public bool IsMaoDe10Active(int team1Score, int team2Score)
        {
            return team1Score >= MaoDe10Threshold || team2Score >= MaoDe10Threshold;
        }

        public int GetPointsUntilWin(int teamScore)
        {
            return Math.Max(0, WinningScore - teamScore);
        }

        public int GetMaxPossibleHandPoints(int team1Score, int team2Score)
        {
            if (IsMaoDe10Active(team1Score, team2Score))
            {
                return 4; // Mão de 10 is active, all hands worth 4 points
            }
            
            return 12; // Maximum possible points from a hand (Doze)
        }

        public bool CanTeamWinInOneHand(int teamScore, int opponentScore)
        {
            var pointsNeeded = GetPointsUntilWin(teamScore);
            var maxHandPoints = GetMaxPossibleHandPoints(teamScore, opponentScore);
            
            return pointsNeeded <= maxHandPoints;
        }

        public GameRiskLevel CalculateGameRiskLevel(int team1Score, int team2Score, int currentStake)
        {
            var isMaoDe10 = IsMaoDe10Active(team1Score, team2Score);
            var pointsAtRisk = CalculateHandPoints(currentStake, team1Score, team2Score);
            
            // High risk if either team can win with current hand
            if (CanTeamWinInOneHand(team1Score, team2Score) || CanTeamWinInOneHand(team2Score, team1Score))
            {
                return GameRiskLevel.High;
            }
            
            // Medium risk if in Mão de 10 or high stakes
            if (isMaoDe10 || pointsAtRisk >= 6)
            {
                return GameRiskLevel.Medium;
            }
            
            // Low risk for early game low stakes
            return GameRiskLevel.Low;
        }

        public ScoreAnalysis AnalyzeGameState(int team1Score, int team2Score, int currentStake)
        {
            return new ScoreAnalysis
            {
                Team1Score = team1Score,
                Team2Score = team2Score,
                CurrentStake = currentStake,
                IsGameComplete = IsGameComplete(team1Score, team2Score),
                WinningTeam = GetWinningTeam(team1Score, team2Score),
                IsMaoDe10Active = IsMaoDe10Active(team1Score, team2Score),
                HandPoints = CalculateHandPoints(currentStake, team1Score, team2Score),
                Team1WinPercentage = CalculateWinPercentage(team1Score, team2Score),
                Team2WinPercentage = CalculateWinPercentage(team2Score, team1Score),
                RiskLevel = CalculateGameRiskLevel(team1Score, team2Score, currentStake),
                Team1PointsToWin = GetPointsUntilWin(team1Score),
                Team2PointsToWin = GetPointsUntilWin(team2Score),
                Team1CanWinInOneHand = CanTeamWinInOneHand(team1Score, team2Score),
                Team2CanWinInOneHand = CanTeamWinInOneHand(team2Score, team1Score)
            };
        }
    }

    public enum GameRiskLevel
    {
        Low,
        Medium,
        High
    }

    public class ScoreAnalysis
    {
        public int Team1Score { get; set; }
        public int Team2Score { get; set; }
        public int CurrentStake { get; set; }
        public bool IsGameComplete { get; set; }
        public int? WinningTeam { get; set; }
        public bool IsMaoDe10Active { get; set; }
        public int HandPoints { get; set; }
        public double Team1WinPercentage { get; set; }
        public double Team2WinPercentage { get; set; }
        public GameRiskLevel RiskLevel { get; set; }
        public int Team1PointsToWin { get; set; }
        public int Team2PointsToWin { get; set; }
        public bool Team1CanWinInOneHand { get; set; }
        public bool Team2CanWinInOneHand { get; set; }
    }
}
