using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{    
    /// <summary>
    /// Implementation of Truco rules and special game mechanics
    /// </summary>
    public class TrucoRulesEngine : ITrucoRulesEngine
    {        
        public bool CanCallTruco(GameState game, int playerSeat)
        {
            // Cannot call/raise if:
            // 1. Game is not active
            if (game.GameStatus != "active")
                return false;

            // 2. Both teams are at 10 points ("Mão de 10" - truco disabled)
            if (game.IsBothTeamsAt10)
                return false;

            // 3. Already at maximum truco level
            if (game.TrucoCallState == TrucoCallState.Doze)
                return false;

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
                return false;

            // 4. Same team cannot call/raise consecutively
            var playerTeam = (int)player.Team;
            if (game.LastTrucoCallerTeam == playerTeam)
                return false;

            return true;
        }

        public bool CanRaise(GameState game, int playerSeat)
        {
            // Raising is the same as calling truco in our unified system
            return CanCallTruco(game, playerSeat) && game.TrucoCallState != TrucoCallState.None;
        }

        public bool CanAcceptTruco(GameState game, int playerSeat)
        {
            // Cannot accept if no truco call is pending
            if (game.TrucoCallState == TrucoCallState.None)
                return false;

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
                return false;

            // Cannot accept your own team's call
            var playerTeam = (int)player.Team;
            return game.LastTrucoCallerTeam != playerTeam;
        }

        public bool CanSurrenderTruco(GameState game, int playerSeat)
        {
            // Same logic as accepting - must be able to respond to the call
            return CanAcceptTruco(game, playerSeat);
        }        public bool IsMaoDe10Active(GameState game)
        {
            // "Mão de 10" is active when any team has exactly 10 points
            return game.TeamScores.Values.Any(score => score == 10);
        }

        public bool IsOneTeamAt10(GameState game)
        {
            // Check if exactly one team has 10 points
            var teamsAt10 = game.TeamScores.Values.Count(score => score == 10);
            return teamsAt10 == 1;
        }

        public bool AreBothTeamsAt10(GameState game)
        {
            // Check if both teams have 10 points
            var teamsAt10 = game.TeamScores.Values.Count(score => score == 10);
            return teamsAt10 == 2;
        }        public Team? GetTeamAt10(GameState game)
        {
            // Returns the team that has 10 points, or null if none/both have 10
            if (AreBothTeamsAt10(game))
                return null; // When both teams are at 10, return null
                
            foreach (var teamScore in game.TeamScores)
            {
                if (teamScore.Value == 10)
                    return teamScore.Key;
            }
            return null;
        }public void ApplyMaoDe10Rule(GameState game)
        {           
            if (AreBothTeamsAt10(game))
            {
                // Case 2: Both teams at 10 - normal hand, no truco allowed
                game.IsBothTeamsAt10 = true;
                game.Stakes = TrucoConstants.Stakes.Initial; // Normal 2-point hand
                game.TrucoCallState = TrucoCallState.None;
                game.LastTrucoCallerTeam = -1;
                game.CanRaiseTeam = null;
                
                game.ActionLog.Add(new ActionLogEntry("game-event")
                {
                    Action = "Both teams at 10 points - hand worth 2 points, truco disabled"
                });
            }
            else if (IsOneTeamAt10(game))
            {
                // Case 1: One team at 10 - automatic truco state
                var teamAt10 = GetTeamAt10(game);
                if (teamAt10.HasValue)
                {
                    game.Stakes = TrucoConstants.Stakes.TrucoCall; // 4 points
                    game.TrucoCallState = TrucoCallState.Truco;
                    game.LastTrucoCallerTeam = (int)teamAt10.Value;
                    game.CanRaiseTeam = null; // No raises allowed in "Mão de 10"
                    game.IsBothTeamsAt10 = false; // Explicitly set for clarity
                    
                    game.ActionLog.Add(new ActionLogEntry("game-event")
                    {
                        Action = $"Mão de 10 activated - Team {teamAt10.Value} at 10 points, hand automatically worth 4 points"
                    });
                }
            }
        }

        public int CalculateStakes(GameState game)
        {
            // Check for "Mão de 10" first
            if (IsMaoDe10Active(game))
            {
                return 4;
            }            // Regular stakes calculation
            if (game.TrucoCallState == TrucoCallState.None)
            {
                return 2; // Base hand value (updated from 1 to 2 per new rules)
            }

            return game.Stakes;
        }

        public int CalculateHandPoints(GameState game)
        {
            return CalculateStakes(game);
        }
    }
}
