using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Implementation of Truco rules and special game mechanics
    /// </summary>
    public class TrucoRulesEngine : ITrucoRulesEngine
    {
        public bool ProcessTrucoCall(GameState game, string playerId)
        {
            if (!CanCallTruco(game, playerId))
                return false;

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                return false;            // First Truco call raises stakes from 2 to 4 points
            if (!game.IsTrucoCalled)
            {
                game.Stakes = TrucoConstants.Stakes.TrucoCall;
                game.IsTrucoCalled = true;
            }

            // Log the action
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerId = playerId,
                Action = $"{player.Name} called Truco (stakes now {game.Stakes})"
            });

            return true;
        }

        public bool ProcessRaise(GameState game, string playerId)
        {
            if (!CanRaise(game, playerId))
                return false;

            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                return false;            // Raise stakes by 4 points (4->8, 8->12)
            var newStakes = game.Stakes + TrucoConstants.Stakes.RaiseAmount;
            if (newStakes <= TrucoConstants.Stakes.Maximum)
            {
                game.Stakes = newStakes;
                
                // Log the action
                game.ActionLog.Add(new ActionLogEntry("button-pressed")
                {
                    PlayerId = playerId,
                    Action = $"{player.Name} raised stakes to {game.Stakes}"
                });

                return true;
            }

            return false;
        }

        public bool ProcessFold(GameState game, string playerId)
        {
            var player = game.Players.FirstOrDefault(p => p.Id == playerId);
            if (player == null)
                return false;            // Award points to opposing team
            string opposingTeam = player.Team == TrucoConstants.Teams.PlayerTeam ? TrucoConstants.Teams.OpponentTeam : TrucoConstants.Teams.PlayerTeam;
            int pointsToAward = Math.Max(1, game.Stakes);
            
            if (!game.TeamScores.ContainsKey(opposingTeam))
                game.TeamScores[opposingTeam] = 0;
            
            game.TeamScores[opposingTeam] += pointsToAward;

            // Log the fold
            game.ActionLog.Add(new ActionLogEntry("button-pressed")
            {
                PlayerId = playerId,
                Action = $"{player.Name} folded, {opposingTeam} gains {pointsToAward} points"
            });

            // Log hand result
            game.ActionLog.Add(new ActionLogEntry("hand-result")
            {
                HandNumber = game.CurrentHand,
                Winner = opposingTeam,
                WinnerTeam = opposingTeam
            });

            return true;
        }

        public bool CanCallTruco(GameState game, string playerId)
        {            // Can only call Truco if:
            // 1. Truco hasn't been called yet, OR
            // 2. Player is responding to opponent's Truco/raise
            // 3. Stakes are not at maximum (12)
            return game.IsRaiseEnabled && game.Stakes < TrucoConstants.Stakes.Maximum;
        }

        public bool CanRaise(GameState game, string playerId)
        {
            // Can raise if Truco has been called and stakes are below maximum
            return game.IsTrucoCalled && game.IsRaiseEnabled && game.Stakes < TrucoConstants.Stakes.Maximum;
        }

        public bool IsMaoDe10Active(GameState game)
        {
            // "Mão de 10" is active when either team has exactly 10 points
            return game.TeamScores.Values.Any(score => score == 10);
        }

        public void ApplyMaoDe10Rule(GameState game)
        {            if (IsMaoDe10Active(game))
            {
                // In "Mão de 10", the hand is automatically worth 4 points
                game.Stakes = TrucoConstants.Stakes.TrucoCall;
                game.IsTrucoCalled = true; // Considered as if Truco was automatically called
                
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
            }

            // Regular stakes calculation
            if (!game.IsTrucoCalled)
            {
                return 1; // Base hand value
            }

            return game.Stakes;
        }

        public int CalculateHandPoints(GameState game)
        {
            return CalculateStakes(game);
        }
    }
}
