using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Represents the current state of the Truco game
    /// </summary>
    public class GameStateDto
    {
        /// <summary>
        /// All players in the game
        /// </summary>
        public List<PlayerDto> Players { get; set; } = new List<PlayerDto>();

        /// <summary>
        /// Cards played in the current round (one per seat)
        /// </summary>
        public List<PlayedCardDto> PlayedCards { get; set; } = new List<PlayedCardDto>();

        /// <summary>
        /// Current points at stake in the round
        /// </summary>
        public int Stakes { get; set; }

        /// <summary>
        /// Whether Truco has been called in the current round
        /// </summary>
        public bool IsTrucoCalled { get; set; }

        /// <summary>
        /// Whether raising the stakes is currently allowed
        /// </summary>
        public bool IsRaiseEnabled { get; set; }        /// <summary>
        /// The current hand number in the match
        /// </summary>
        public int CurrentHand { get; set; }

        /// <summary>
        /// Winners of each round in the current hand (by team number: 1 or 2)
        /// Index corresponds to round number (0-based)
        /// Example: [1, 2] means Team 1 won round 1, Team 2 won round 2, currently in round 3
        /// </summary>
        public List<int> RoundWinners { get; set; } = new List<int>();        
        /// <summary>
        /// The scores for each team (team numbers as keys, scores as values)
        /// Team 1 and Team 2 represented as "1" and "2"
        /// </summary>
        public Dictionary<Team, int> TeamScores { get; set; } = new Dictionary<Team, int>();

        /// <summary>
        /// Whether the game has been completed (one team reached 12 points)
        /// </summary>
        public bool IsGameComplete { get; set; }

        /// <summary>
        /// The winning team if the game is completed (1 or 2), null otherwise
        /// Only serialized when not null
        /// </summary>
        public int? WinningTeam { get; set; }        /// <summary>
        /// Log of actions that have occurred in the game
        /// </summary>
        public List<ActionLogEntryDto> ActionLog { get; set; } = new List<ActionLogEntryDto>();
    }
}
