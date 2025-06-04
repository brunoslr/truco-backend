namespace TrucoMineiro.API.Models
{
    /// <summary>
    /// Represents an action in the game log
    /// </summary>
    public class ActionLogEntry
    {
        /// <summary>
        /// The type of action (e.g., "card-played", "button-pressed", "hand-result", "turn-result")
        /// </summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the player who performed the action (optional, depending on type)
        /// </summary>
        public string? PlayerId { get; set; }

        /// <summary>
        /// The card that was played (optional, for "card-played" type)
        /// </summary>
        public string? Card { get; set; }

        /// <summary>
        /// The action that was performed (optional, for "button-pressed" type)
        /// </summary>
        public string? Action { get; set; }

        /// <summary>
        /// The hand number (optional, for "hand-result" type)
        /// </summary>
        public int? HandNumber { get; set; }

        /// <summary>
        /// The winner (optional, for "hand-result" or "turn-result" type)
        /// </summary>
        public string? Winner { get; set; }

        /// <summary>
        /// The winning team (optional, for "turn-result" type)
        /// </summary>
        public string? WinnerTeam { get; set; }

        public ActionLogEntry(string type)
        {
            Type = type;
        }

        public ActionLogEntry() { }
    }

    /// <summary>
    /// Represents a card played by a player during a round
    /// </summary>
    public class PlayedCard
    {
        /// <summary>
        /// The player who played this card
        /// </summary>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>
        /// The card that was played
        /// </summary>
        public Card? Card { get; set; }

        public PlayedCard(string playerId, Card? card = null)
        {
            PlayerId = playerId;
            Card = card;
        }

        public PlayedCard() { }
    }

    /// <summary>
    /// Represents the state of a Truco Mineiro game
    /// </summary>
    public class GameState
    {
        /// <summary>
        /// Unique identifier for the game
        /// </summary>
        public string GameId { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// All players in the game
        /// </summary>
        public List<Player> Players { get; set; } = new List<Player>();

        /// <summary>
        /// Cards played in the current round
        /// </summary>
        public List<PlayedCard> PlayedCards { get; set; } = new List<PlayedCard>();

        /// <summary>
        /// Current points at stake in the round
        /// </summary>
        public int Stakes { get; set; } = 1;

        /// <summary>
        /// Whether Truco has been called in the current round
        /// </summary>
        public bool IsTrucoCalled { get; set; }

        /// <summary>
        /// Whether raising the stakes is currently allowed
        /// </summary>
        public bool IsRaiseEnabled { get; set; } = true;

        /// <summary>
        /// The current hand number in the match
        /// </summary>
        public int CurrentHand { get; set; } = 1;

        /// <summary>
        /// The scores for each team
        /// </summary>
        public Dictionary<string, int> TeamScores { get; set; } = new Dictionary<string, int>();

        /// <summary>
        /// The team that won the current turn, or null if undecided
        /// </summary>
        public string? TurnWinner { get; set; }

        /// <summary>
        /// Log of actions that have occurred in the game
        /// </summary>
        public List<ActionLogEntry> ActionLog { get; set; } = new List<ActionLogEntry>();

        /// <summary>
        /// The current dealer's seat
        /// </summary>
        public int DealerSeat { get; set; } = 0;

        /// <summary>
        /// Seat number of the first player for the current hand
        /// </summary>
        public int FirstPlayerSeat { get; set; } = 1;

        /// <summary>
        /// The deck of cards for the game
        /// </summary>
        public Deck Deck { get; set; } = new Deck();

        /// <summary>
        /// The maximum number of players (typically 4 for Truco)
        /// </summary>
        public const int MaxPlayers = 4;

        /// <summary>
        /// The winning score (typically 12 points)
        /// </summary>
        public const int WinningScore = 12;

        public GameState()
        {
            // Initialize team scores
            TeamScores["Player's Team"] = 0;
            TeamScores["Opponent Team"] = 0;
        }

        /// <summary>
        /// Initialize the game with players
        /// </summary>
        public void InitializeGame()
        {
            // Create players if they don't exist
            if (Players.Count < MaxPlayers)
            {
                Players = new List<Player>
                {
                    new Player("You", "Player's Team", 0),
                    new Player("AI 1", "Opponent Team", 1),
                    new Player("Partner", "Player's Team", 2),
                    new Player("AI 2", "Opponent Team", 3)
                };
            }

            // Set the dealer and first player
            Players[DealerSeat].IsDealer = true;
            Players[FirstPlayerSeat].IsActive = true;

            // Initialize the played cards for each seat
            PlayedCards = Players.Select(p => new PlayedCard(p.Id)).ToList();

            // Reset the game state
            ResetRound();
        }

        /// <summary>
        /// Initialize the game with players
        /// </summary>
        /// <param name="playerName">Custom name for the player at seat 0</param>
        public void InitializeGame(string playerName)
        {
            // Create players if they don't exist
            if (Players.Count < MaxPlayers)
            {
                Players = new List<Player>
                {
                    new Player(playerName, "Player's Team", 0),
                    new Player("AI 1", "Opponent Team", 1),
                    new Player("Partner", "Player's Team", 2),
                    new Player("AI 2", "Opponent Team", 3)
                };
            }
            else
            {
                // Update the name of the player at seat 0
                var player = Players.FirstOrDefault(p => p.Seat == 0);
                if (player != null)
                {
                    player.Name = playerName;
                }
            }

            // Set the dealer and first player
            Players[DealerSeat].IsDealer = true;
            Players[FirstPlayerSeat].IsActive = true;

            // Initialize the played cards for each seat
            PlayedCards = Players.Select(p => new PlayedCard(p.Id)).ToList();

            // Reset the game state
            ResetRound();
        }

        /// <summary>
        /// Reset the round state
        /// </summary>
        public void ResetRound()
        {
            // Clear all players' hands
            foreach (var player in Players)
            {
                player.ClearHand();
            }

            // Reset the played cards
            foreach (var playedCard in PlayedCards)
            {
                playedCard.Card = null;
            }

            // Reset the deck and shuffle
            Deck = new Deck();
            Deck.Shuffle();

            // Deal cards to the players
            DealCards();

            // Reset the stakes and truco status
            Stakes = 1;
            IsTrucoCalled = false;
            IsRaiseEnabled = true;
            TurnWinner = null;
        }

        /// <summary>
        /// Deal cards to all players
        /// </summary>
        public void DealCards()
        {
            // Deal 3 cards to each player
            foreach (var player in Players)
            {
                var cards = Deck.DrawCards(3);
                foreach (var card in cards)
                {
                    player.AddCard(card);
                }
            }
        }

        /// <summary>
        /// Move to the next hand
        /// </summary>
        public void NextHand()
        {
            CurrentHand++;
            
            // Update the dealer and first player for the next hand
            DealerSeat = (DealerSeat + 1) % MaxPlayers;
            FirstPlayerSeat = (DealerSeat + 1) % MaxPlayers;
            
            // Reset player states
            foreach (var player in Players)
            {
                player.IsDealer = player.Seat == DealerSeat;
                player.IsActive = player.Seat == FirstPlayerSeat;
            }
            
            ResetRound();
            
            // Log the start of a new hand
            ActionLog.Add(new ActionLogEntry("hand-start") { HandNumber = CurrentHand });
        }

        /// <summary>
        /// Add points to a team's score
        /// </summary>
        public void AddScore(string team, int points)
        {
            if (TeamScores.ContainsKey(team))
            {
                TeamScores[team] += points;
                
                // Check for game winner
                if (TeamScores[team] >= WinningScore)
                {
                    ActionLog.Add(new ActionLogEntry("game-result") { WinnerTeam = team });
                }
            }
        }
    }
}
