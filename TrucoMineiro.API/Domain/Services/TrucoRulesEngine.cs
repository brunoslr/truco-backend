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
        }

        public bool IsMaoDe10Active(GameState game)
        {
            // "Mão de 10" is active when either team has exactly 10 points
            return game.TeamScores.Values.Any(score => score == 10);
        }        public void ApplyMaoDe10Rule(GameState game)
        {           
            if (IsMaoDe10Active(game))
            {
                // In "Mão de 10", the hand is automatically worth 4 points
                game.Stakes = TrucoConstants.Stakes.TrucoCall;
                game.TrucoCallState = TrucoCallState.Truco; // Considered as if Truco was automatically called
                
                game.ActionLog.Add(new ActionLogEntry("game-event")
                {
                    Action = "Mão de 10 activated - hand automatically worth 4 points"
                });
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
