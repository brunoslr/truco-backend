using System.Text.Json.Serialization;

namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Response for a new Truco game
    /// </summary>
    public class StartGameResponse
    {
        /// <summary>
        /// The unique identifier for the game
        /// </summary>
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// The seat number of the player (always 0 for single-player)
        /// </summary>
        public int PlayerSeat { get; set; }

        /// <summary>
        /// The teams in the game
        /// </summary>
        public List<TeamDto> Teams { get; set; } = new List<TeamDto>();

        /// <summary>
        /// All players in the game
        /// </summary>
        public List<PlayerInfoDto> Players { get; set; } = new List<PlayerInfoDto>();

        /// <summary>
        /// The player's current hand of cards
        /// </summary>
        public List<CardDto> Hand { get; set; } = new List<CardDto>();

        /// <summary>
        /// The seat of the current dealer
        /// </summary>
        public int DealerSeat { get; set; }

        /// <summary>
        /// The current scores for each team
        /// </summary>
        [JsonPropertyName("teamScores")]
        public Dictionary<string, int> TeamScores { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// Current points at stake
        /// </summary>
        public int Stakes { get; set; }

        /// <summary>
        /// The current hand number
        /// </summary>
        public int CurrentHand { get; set; }

        /// <summary>
        /// Log of actions that have occurred in the game
        /// </summary>
        public List<ActionLogEntryDto> Actions { get; set; } = new List<ActionLogEntryDto>();
    }

    /// <summary>
    /// Information about a team
    /// </summary>
    public class TeamDto
    {
        /// <summary>
        /// The name of the team
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The seat numbers of players in this team
        /// </summary>
        public List<int> Seats { get; set; } = new List<int>();
    }

    /// <summary>
    /// Basic player information
    /// </summary>
    public class PlayerInfoDto
    {
        /// <summary>
        /// The seat number of the player
        /// </summary>
        public int Seat { get; set; }

        /// <summary>
        /// The name of the player
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The team name of the player
        /// </summary>
        public string Team { get; set; } = string.Empty;
    }
}
