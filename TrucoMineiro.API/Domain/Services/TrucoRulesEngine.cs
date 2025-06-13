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
        public bool ProcessTrucoCall(GameState game, int playerSeat)
        {
            if (!CanCallTruco(game, playerSeat))
                return false;

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
                return false;            // First Truco call raises stakes from 2 to 4 points
            if (game.TrucoCallState == TrucoCallState.None)
            {
                game.Stakes = TrucoConstants.Stakes.TrucoCall;
                game.TrucoCallState = TrucoCallState.Truco;
            }

            // Log the action
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerSeat = player.Seat,
                Action = $"{player.Name} called Truco (stakes now {game.Stakes})"
            });

            return true;
        }

        public bool ProcessRaise(GameState game, int playerSeat)
        {
            if (!CanRaise(game, playerSeat))
                return false;

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);            if (player == null)
                return false;

            // Raise stakes by 4 points (4->8, 8->12)
            var newStakes = game.Stakes + TrucoConstants.Stakes.RaiseAmount;
            if (newStakes <= TrucoConstants.Stakes.Maximum)
            {
                game.Stakes = newStakes;
                
                // Log the action
                game.ActionLog.Add(new ActionLogEntry("button-pressed")
                {
                    PlayerSeat = player.Seat,
                    Action = $"{player.Name} raised stakes to {game.Stakes}"
                });

                return true;
            }

            return false;
        }       
        
        public bool ProcessSurrender(GameState game, int playerSeat)
        {
            var surrenderPlayer = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (surrenderPlayer == null)
                return false;            
            
            // Award points to opposing team
            Team teamReceivingPoints = surrenderPlayer.Team == Team.PlayerTeam ? Team.OpponentTeam : Team.PlayerTeam;
            int pointsToAward = Math.Max(1, game.Stakes);
            
            if (!game.TeamScores.ContainsKey(teamReceivingPoints))
                game.TeamScores[teamReceivingPoints] = 0;
            
            game.TeamScores[teamReceivingPoints] += pointsToAward;
            // Log the surrender
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerSeat = surrenderPlayer.Seat,
                Action = $"{surrenderPlayer.Name} surrenders, {teamReceivingPoints} gains {pointsToAward} points"
            });

            // Log hand result
            game.ActionLog.Add(new ActionLogEntry("hand-result")
            {
                HandNumber = game.CurrentHand,
                Winner = teamReceivingPoints,
                WinnerTeam = teamReceivingPoints
            });

            return true;
        }        
          public bool CanCallTruco(GameState game, int playerSeat)
        {
            // Can only call Truco if:
            // 1. No truco call in progress AND stakes are not at maximum
            // 2. OR player can raise from current state
            return (game.TrucoCallState == TrucoCallState.None || CanRaise(game, playerSeat)) 
                   && game.Stakes < TrucoConstants.Stakes.Maximum 
                   && !game.IsBothTeamsAt10;
        }

        public bool CanRaise(GameState game, int playerSeat)
        {
            // Can raise if there's a truco call in progress and stakes are below maximum
            return game.TrucoCallState != TrucoCallState.None 
                   && game.TrucoCallState != TrucoCallState.Doze // Can't raise beyond Doze
                   && game.Stakes < TrucoConstants.Stakes.Maximum
                   && !game.IsBothTeamsAt10;
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
