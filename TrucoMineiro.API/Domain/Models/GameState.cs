using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.Models
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
        /// The seat of the player who performed the action (optional, depending on type)
        /// </summary>
        public int? PlayerSeat { get; set; }

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
        /// The seat number of the player who played this card (0-3)
        /// </summary>
        public int PlayerSeat { get; set; }        /// <summary>
        /// The card that was played (never null - use Card.CreateEmptyCard() for empty slots)
        /// </summary>
        public Card Card { get; set; } = Card.CreateEmptyCard();        public PlayedCard(int playerSeat, Card card)
        {
            PlayerSeat = playerSeat;
            Card = card;
        }

        public PlayedCard(int playerSeat)
        {
            PlayerSeat = playerSeat;
            Card = Card.CreateEmptyCard();
        }

        public PlayedCard() { }
    }

    /// <summary>
    /// Represents the state of a Truco Mineiro game
    /// </summary>
    public class GameState
    {        /// <summary>
        /// Unique identifier for the game
        /// </summary>
        public string GameId { get; private set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// Alternative ID property for domain services compatibility
        /// </summary>
        public string Id 
        { 
            get => GameId; 
            set => GameId = value; 
        }

        /// <summary>
        /// Current player index for turn tracking
        /// </summary>
        public int CurrentPlayerIndex { get; set; } = 0;        /// <summary>
        /// Team 1 score (seats 0 and 2)
        /// </summary>
        public int Team1Score 
        { 
            get => TeamScores.ContainsKey(TrucoConstants.Teams.PlayerTeam) ? TeamScores[TrucoConstants.Teams.PlayerTeam] : 0;
            set => TeamScores[TrucoConstants.Teams.PlayerTeam] = value;
        }

        /// <summary>
        /// Team 2 score (seats 1 and 3)
        /// </summary>
        public int Team2Score 
        { 
            get => TeamScores.ContainsKey(TrucoConstants.Teams.OpponentTeam) ? TeamScores[TrucoConstants.Teams.OpponentTeam] : 0;
            set => TeamScores[TrucoConstants.Teams.OpponentTeam] = value;
        }

        /// <summary>
        /// Current stake for this hand
        /// </summary>
        public int CurrentStake 
        { 
            get => Stakes; 
            set => Stakes = value; 
        }

        /// <summary>
        /// Whether there's a pending Truco call
        /// </summary>
        public bool PendingTrucoCall { get; set; } = false;

        /// <summary>
        /// Seat of the player who called Truco
        /// </summary>
        public int? TrucoCallerSeat { get; set; }

        /// <summary>
        /// Last response to a Truco call
        /// </summary>
        public TrucoResponse? LastTrucoResponse { get; set; }

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
        public int Stakes { get; set; } = TrucoConstants.Stakes.Initial;

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
        public int DealerSeat { get; set; } = GameConfiguration.InitialDealerSeat;        
        /// <summary>
        /// Seat number of the first player for the current hand (computed based on dealer seat)
        /// The first player is always the one to the left of the dealer (next seat clockwise)
        /// </summary>
        public int FirstPlayerSeat => GameConfiguration.GetFirstPlayerSeat(DealerSeat);

        /// <summary>
        /// The deck of cards for the game
        /// </summary>
        public Deck Deck { get; set; } = new Deck();

        /// <summary>
        /// Timestamp of when the game was created
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Timestamp of the last activity in the game
        /// </summary>
        public DateTime LastActivity { get; set; } = DateTime.UtcNow;
        /// <summary>
        /// Current round number within the hand (1, 2, or 3)
        /// </summary>
        public int CurrentRound { get; set; } = 1;

        /// <summary>
        /// Track cards played in each round of the current hand
        /// Key: Round number (1-3), Value: PlayedCards for that round
        /// </summary>
        public Dictionary<int, List<PlayedCard>> RoundHistory { get; set; } = new();

        /// <summary>
        /// Winners of each round in the current hand (by team number)
        /// </summary>
        public List<int> RoundWinners { get; set; } = new List<int>();        
        /// <summary>
        /// Whether the game is completed (someone reached 12 points)
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// The winning team if the game is completed (1 or 2)
        /// </summary>
        public int? WinningTeam { get; set; }        /// <summary>
        /// Current game status using proper enum
        /// </summary>
        public GameStatus Status { get; set; } = Models.GameStatus.Waiting;

        /// <summary>
        /// Current game status as string (for backward compatibility)
        /// </summary>
        public string GameStatus 
        { 
            get => Status.ToString().ToLower();            set 
            { 
                if (Enum.TryParse<GameStatus>(value, true, out var status))
                    Status = status;
            }
        }

        /// <summary>
        /// Current Truco level (1 = Truco, 3 = Seis, 6 = Nove, 9 = Doze, 12 = maximum)
        /// </summary>
        public int TrucoLevel { get; set; } = 1;        /// <summary>
        /// ID of the player who called Truco
        /// </summary>
        public Guid? TrucoCalledBy { get; set; }

        /// <summary>
        /// Whether we're waiting for a Truco response
        /// </summary>
        public bool WaitingForTrucoResponse { get; set; } = false;

        /// <summary>
        /// The maximum number of players (typically 4 for Truco)
        /// </summary>
        public const int MaxPlayers = TrucoConstants.Game.MaxPlayers;

        /// <summary>
        /// The winning score (typically 12 points)
        /// </summary>
        public const int WinningScore = TrucoConstants.Game.WinningScore;        public GameState()
        {
            // Initialize team scores
            TeamScores[TrucoConstants.Teams.PlayerTeam] = 0;
            TeamScores[TrucoConstants.Teams.OpponentTeam] = 0;
        }

        /// <summary>
        /// Initialize the game with players
        /// </summary>
        public void InitializeGame()
        {
            // Create players if they don't exist
            if (Players.Count < MaxPlayers)
            {                Players = new List<Player>
                {
                    new Player("You", TrucoConstants.Teams.PlayerTeam, 0),
                    new Player("AI 1", TrucoConstants.Teams.OpponentTeam, 1),
                    new Player("Partner", TrucoConstants.Teams.PlayerTeam, 2),                    new Player("AI 2", TrucoConstants.Teams.OpponentTeam, 3)
                };
            }            // Set the dealer and first player
            Players[DealerSeat].IsDealer = true;
            Players[FirstPlayerSeat].IsActive = true;

            // Initialize the played cards as empty collection
            PlayedCards = new List<PlayedCard>();

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
            {                Players = new List<Player>
                {
                    new Player(playerName, TrucoConstants.Teams.PlayerTeam, 0),
                    new Player("AI 1", TrucoConstants.Teams.OpponentTeam, 1),
                    new Player("Partner", TrucoConstants.Teams.PlayerTeam, 2),
                    new Player("AI 2", TrucoConstants.Teams.OpponentTeam, 3)
                };
            }
            else
            {
                // Update the name of the player at seat 0
                var player = Players.FirstOrDefault(p => p.Seat == 0);
                if (player != null)                {
                    player.Name = playerName;
                }
            }            // Set the dealer and first player
            Players[DealerSeat].IsDealer = true;
            Players[FirstPlayerSeat].IsActive = true;

            // Initialize the played cards as empty collection
            PlayedCards = new List<PlayedCard>();

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
            }            // Reset the played cards
            foreach (var playedCard in PlayedCards)
            {
                playedCard.Card = Card.CreateEmptyCard();
            }

            // Reset the deck and shuffle
            Deck = new Deck();
            Deck.Shuffle();

            // Deal cards to the players
            DealCards();            // Reset the stakes and truco status
            Stakes = TrucoConstants.Stakes.Initial;
            IsTrucoCalled = false;
            IsRaiseEnabled = true;
            TurnWinner = null;
        }

        /// <summary>
        /// Deal cards to all players
        /// </summary>
        public void DealCards()
        {            // Deal 3 cards to each player
            foreach (var player in Players)
            {
                var cards = Deck.DrawCards(TrucoConstants.Game.CardsPerPlayer);
                foreach (var card in cards)
                {
                    player.AddCard(card);
                }
            }
        }        /// <summary>
        /// Move to the next hand
        /// </summary>
        public void NextHand()
        {
            CurrentHand++;
              // Update the dealer and first player for the next hand
            // In Truco Mineiro, the dealer is known as the "Pé" (foot in Portuguese)
            // The dealer moves to the left at the end of each hand
            DealerSeat = GameConfiguration.GetNextDealerSeat(DealerSeat);
            // FirstPlayerSeat is now computed automatically based on DealerSeat
            
            // Reset player states
            foreach (var player in Players)
            {
                player.IsDealer = player.Seat == DealerSeat;
                player.IsActive = player.Seat == FirstPlayerSeat;
            }
            
            // Update current player index
            CurrentPlayerIndex = FirstPlayerSeat;
            
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

        /// <summary>
        /// Start the game by setting status to active
        /// </summary>
        public void StartGame()
        {
            GameStatus = "active";
            LastActivity = DateTime.UtcNow;
            
            // Set first player as active
            foreach (var player in Players)
            {
                player.IsActive = player.Seat == FirstPlayerSeat;
            }
            
            ActionLog.Add(new ActionLogEntry("game-started"));
        }

        /// <summary>
        /// Get the current active player
        /// </summary>
        public Player? GetCurrentPlayer()
        {
            return Players.FirstOrDefault(p => p.Seat == CurrentPlayerIndex);
        }

        /// <summary>
        /// Get the next player in turn order
        /// </summary>
        public Player? GetNextPlayer()
        {
            var nextIndex = (CurrentPlayerIndex + 1) % MaxPlayers;
            return Players.FirstOrDefault(p => p.Seat == nextIndex);
        }
    }
}
