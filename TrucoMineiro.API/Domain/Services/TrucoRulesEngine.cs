using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{    
    /// <summary>
    /// Implementation of Truco rules and special game mechanics
    /// </summary>
    public class TrucoRulesEngine : ITrucoRulesEngine
    {          public bool CanCallTruco(GameState game, int playerSeat)
        {
            // Cannot call/raise if:
            // 1. Game is not active
            if (game.GameStatus != "active")
                return false;

            // 2. Both teams are at last hand (truco disabled)
            if (AreBothTeamsAtLastHand(game))
                return false;

            // 3. Already at maximum truco level
            if (IsMaximumStakes(game))
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
        }        public bool CanSurrenderTruco(GameState game, int playerSeat)
        {
            // Same logic as accepting - must be able to respond to the call
            return CanAcceptTruco(game, playerSeat);
        }

        // NEW DYNAMIC RULE METHODS

        public bool IsLastHand(GameState game)
        {
            // Last hand is when any team has reached one victory away (10+ points)
            return game.TeamScores.Values.Any(score => score >= TrucoConstants.Game.WinningScore - 2);
        }

        public bool IsOneTeamAtLastHand(GameState game)
        {
            // Check if exactly one team is one victory away
            var teamsAtLastHand = game.TeamScores.Values.Count(score => score >= TrucoConstants.Game.WinningScore - 2);
            return teamsAtLastHand == 1;
        }

        public bool AreBothTeamsAtLastHand(GameState game)
        {
            // Check if both teams are one victory away
            var teamsAtLastHand = game.TeamScores.Values.Count(score => score >= TrucoConstants.Game.WinningScore - 2);
            return teamsAtLastHand == 2;
        }

        public Team? GetTeamAtLastHand(GameState game)
        {
            // Returns the team that is one victory away, or null if none/both are
            if (AreBothTeamsAtLastHand(game))
                return null; // When both teams are at last hand, return null
                
            foreach (var teamScore in game.TeamScores)
            {
                if (teamScore.Value >= TrucoConstants.Game.WinningScore - 2)
                    return teamScore.Key;
            }
            return null;
        }

        public bool IsMaximumStakes(GameState game)
        {
            return game.Stakes >= TrucoConstants.Stakes.Maximum;
        }

        public int GetNextStakes(int currentStakes)
        {
            return TrucoConstants.Stakes.GetNextStakesValue(currentStakes);
        }        public void ApplyLastHandRule(GameState game)
        {
            if (AreBothTeamsAtLastHand(game))
            {
                // Case 2: Both teams at last hand - normal hand, no truco allowed
                game.Stakes = TrucoConstants.Stakes.Initial; // Normal 2-point hand
                game.TrucoCallState = TrucoCallState.None;
                game.LastTrucoCallerTeam = -1;
                game.CanRaiseTeam = null;
                
                game.ActionLog.Add(new ActionLogEntry("game-event")
                {
                    Action = "Both teams at last hand - hand worth 2 points, truco disabled"
                });
            }
            else if (IsOneTeamAtLastHand(game))
            {
                // Case 1: One team at last hand - automatic truco state
                var teamAtLastHand = GetTeamAtLastHand(game);
                if (teamAtLastHand.HasValue)
                {
                    game.Stakes = TrucoConstants.Stakes.TrucoCall; // 4 points
                    game.TrucoCallState = TrucoCallState.Truco;
                    game.LastTrucoCallerTeam = (int)teamAtLastHand.Value;
                    game.CanRaiseTeam = null; // No raises allowed in last hand
                    
                    game.ActionLog.Add(new ActionLogEntry("game-event")
                    {
                        Action = $"Last hand activated - Team {teamAtLastHand.Value} at {game.TeamScores[teamAtLastHand.Value]} points, hand automatically worth 4 points"});
                }
            }
        }

        // EXISTING METHODS (updated to use new logic)

        public int CalculateStakes(GameState game)
        {
            // Check for last hand first (replaces legacy "Mão de 10" logic)
            if (IsLastHand(game))
            {
                if (AreBothTeamsAtLastHand(game))
                    return TrucoConstants.Stakes.Initial; // 2 points
                else if (IsOneTeamAtLastHand(game))
                    return TrucoConstants.Stakes.TrucoCall; // 4 points
            }

            // Regular stakes calculation based on truco state
            if (game.TrucoCallState == TrucoCallState.None)
            {
                return TrucoConstants.Stakes.Initial; // 2 points
            }

            return game.Stakes;
        }

        public int CalculateHandPoints(GameState game)
        {
            return CalculateStakes(game);
        }
    }
}
