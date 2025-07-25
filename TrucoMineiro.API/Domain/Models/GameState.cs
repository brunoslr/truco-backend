using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Domain.Models
{    /// <summary>
    /// Represents an action in the game log
    /// </summary>
    public class ActionLogEntry
    {
        /// <summary>
        /// The type of action (e.g., "card-played", "button-pressed", "hand-result", "turn-result")
        /// </summary>
        public string Type { get; set; } = string.Empty;        
        
        /// <summary>
        /// When this action occurred (UTC timestamp for proper chronological ordering)
        /// </summary>
        public DateTime Timestamp { get; set; }        
        /// <summary>
        /// The current round number within the hand (1, 2, or 3) when this action occurred
        /// </summary>
        public int? RoundNumber { get; set; }

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
        public Team? Winner { get; set; }

        /// <summary>
        /// The winning team (optional, for "turn-result" type)
        /// </summary>
        public Team? WinnerTeam { get; set; }

        public ActionLogEntry(string type)
        {
            Type = type;
            Timestamp = DateTime.UtcNow;
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
        }        /// <summary>
        /// Current player index for turn tracking
        /// </summary>
        public int CurrentPlayerIndex { get; set; } = 0;        /// <summary>
        /// Team 1 score (seats 0 and 2)
        /// </summary>
        public int Team1Score 
        { 
            get => TeamScores.ContainsKey(Team.PlayerTeam) ? TeamScores[Team.PlayerTeam] : 0;
            set => TeamScores[Team.PlayerTeam] = value;
        }

        /// <summary>
        /// Team 2 score (seats 1 and 3)
        /// </summary>
        public int Team2Score 
        { 
            get => TeamScores.ContainsKey(Team.OpponentTeam) ? TeamScores[Team.OpponentTeam] : 0;
            set => TeamScores[Team.OpponentTeam] = value;
        }

        /// <summary>
        /// Current stake for this hand
        /// </summary>
        public int CurrentStake 
        { 
            get => Stakes; 
            set => Stakes = value; 
        }        /// <summary>
        /// Whether there's a pending Truco call (computed from new state model)
        /// </summary>
        public bool PendingTrucoCall 
        { 
            get => TrucoCallState != TrucoCallState.None;
            set { } // Setter kept for backward compatibility but does nothing
        }        /// <summary>
        /// Seat of the player who called Truco (computed from new state model)
        /// </summary>
        public int? TrucoCallerSeat 
        { 
            get 
            {
                if (LastTrucoCallerTeam == -1) return null;
                // Return the first player seat of the team that made the call
                return Players.FirstOrDefault(p => (int)p.Team == LastTrucoCallerTeam)?.Seat;
            }
            set { } // Setter kept for backward compatibility but does nothing
        }

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
        /// Current stake for this hand (same as Stakes, for clarity)
        /// </summary>
        public int CurrentStakes 
        { 
            get => Stakes; 
            set => Stakes = value; 
        }/// <summary>
        /// Current state of Truco calls and raises
        /// </summary>
        public TrucoCallState TrucoCallState { get; set; } = TrucoCallState.None;        /// <summary>
        /// Team ID of the last team that called Truco or raised (-1 = no previous caller)
        /// </summary>
        public int LastTrucoCallerTeam { get; set; } = -1;

        /// <summary>
        /// Team ID that can raise next (null = either team can call truco)
        /// </summary>
        public int? CanRaiseTeam { get; set; } = null;

        /// <summary>
        /// Iron Hand feature: when enabled, players cannot see their own cards during last hand
        /// </summary>
        public bool IronHandEnabled { get; set; } = GameConfiguration.DefaultIronHandEnabled;

        /// <summary>
        /// Partner card visibility: when enabled, teams at last hand can see their partner's cards
        /// </summary>
        public bool PartnerCardVisibilityEnabled { get; set; } = GameConfiguration.DefaultPartnerCardVisibilityEnabled;

        /// <summary>
        /// The current hand number in the match
        /// </summary>
        public int CurrentHand { get; set; } = 1;        /// <summary>
        /// The scores for each team
        /// </summary>
        public Dictionary<Team, int> TeamScores { get; set; } = new Dictionary<Team, int>();

        /// <summary>
        /// The team that won the current turn, or null if undecided
        /// </summary>
        public Team? TurnWinner { get; set; }

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
            }        }

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
            TeamScores[Team.PlayerTeam] = 0;
            TeamScores[Team.OpponentTeam] = 0;
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
                    new Player("You", Team.PlayerTeam, 0),
                    new Player("AI 1", Team.OpponentTeam, 1) { IsAI = true },
                    new Player("Partner", Team.PlayerTeam, 2) { IsAI = true },                    
                    new Player("AI 2", Team.OpponentTeam, 3) { IsAI = true }
                };
            }
            // Set the dealer and first player
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
                    new Player(playerName, Team.PlayerTeam, 0),
                    new Player("AI 1", Team.OpponentTeam, 1) { IsAI = true },
                    new Player("Partner", Team.PlayerTeam, 2) { IsAI = true },
                    new Player("AI 2", Team.OpponentTeam, 3) { IsAI = true }
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

            // Reset Truco state for new hand
            TrucoCallState = TrucoCallState.None;
            LastTrucoCallerTeam = -1;
            CanRaiseTeam = null;
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
        }        /// <summary>
        /// Add points to a team's score
        /// </summary>
        public void AddScore(Team team, int points)
        {
            if (TeamScores.ContainsKey(team))
            {
                TeamScores[team] += points;
                
                // Check for game winner                if (TeamScores[team] >= WinningScore)
                {
                    ActionLog.Add(new ActionLogEntry("game-result") { WinnerTeam = team });
                }
            }
        }        /// <summary>
        /// Start the game by setting status to active
        /// </summary>
        public void StartGame()
        {
            Status = Models.GameStatus.Active;
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
