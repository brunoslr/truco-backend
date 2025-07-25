# Truco Mineiro Backend

## Project Description

This project is a backend implementation for Truco Mineiro, built using .NET and C#. It provides API endpoints that a frontend client can use to interact with the game server.

## Technologies Used

- **.NET**: Cross-platform development framework
- **ASP.NET Core**: Web API framework
- **C#**: Programming language
- **xUnit**: Unit testing framework
- **Docker**: Containerization for deployment

## Rules of Truco Mineiro

### Players and Teams
Truco Mineiro is typically played with two or four players divided into two teams of one or two players each.

### Deck
- The game uses a 40-card deck (standard deck with 8s, 9s, and 10s removed).

### Card Ranking (Highest to Lowest)
1. 4 of Clubs (Zap)
2. 7 of Hearts (Copas)
3. Ace of Spades (Espadão)
4. 7 of Diamonds (Espadinha)
5. 3s, 2s, Aces, Kings, Jacks, Queens, 7 of Spades, 7 of Clubs, 6s, 5s, 4s

### Objective
The objective is to win hands and accumulate points. The first team to reach 12 points wins the game.
l
---

### Turn Structure

#### Dealing Cards
- At the beginning of each hand, the dealer shuffles the deck and deals three cards to each player.
- The dealer rotates clockwise (to the left) after each hand.

#### Playing Cards
- Players take turns playing one card each, starting with the player to the left of the dealer.
- Each player plays one card face-up in the center of the table.

#### Winning Rounds
- The highest-ranked card wins the round.
- The team that wins two out of three rounds wins the hand.

#### Handling Draws
- **Draw in the first round:** All players may show their highest card. The player with the highest card wins the hand. Players can choose to play or fold in this case.
- **Draw in the second or third rounds:** The team that won the first round wins the hand.


## Stakes Progression

| Call/Raise | Points at Stake |
|------------|----------------|
| Initial    | 2              |
| Truco      | 4              |
| Seis       | 8              |
| Nove       | 10             |
| Doze       | 12             |

- **Truco:** When a team calls Truco and the opposing team accepts, the round is now worth 4 points.
- **Seis (Raise):** After Truco is called, only the opposing team can raise to Seis. If accepted, the round is now worth 8 points.
- **Nove (Raise):** After Seis is called, only the opposing team can raise to Nove. If accepted, the round is now worth 10 points.
- **Doze (Raise):** After Nove is called, only the opposing team can raise to Doze. If accepted, the round is now worth 12 points (maximum).
- **Surrender:** If a team surrenders to a call or raise, a surrender event is triggered and the opposing team is awarded the points from the previous stake level.

## Raise Logic and Team Alternation

- A **Raise** (Seis, Nove, or Doze) is always a combination of accepting the current stake and either immediately proposing a higher stake (accept + new Truco back), or raising later in the same hand.
- Once a team calls Truco, the opposing team can either surrender, accept, or raise back (Seis). If they just accept, they retain the possibility to raise (Seis) at any time until the hand is completed.
- After a raise from a team, the opposing team has the opportunity to raise back at any time until the end of the hand.
- The complete progression is: Initial(2) → Truco(4) → Seis(8) → Nove(10) → Doze(12).
- At the start of a new hand, stakes reset to 2 and either team can call Truco first.

## Special Rule: "Last Hand" (Dynamic Calculation)

The "Last Hand" occurs when a team reaches a score where winning the minimum stakes would achieve victory. By default, this happens when a team reaches 10 points (since 10 + 2 = 12, the victory threshold).

### When ONE team is at "Last Hand":
- The team at last hand is **automatically in a truco-like state** before the hand begins.
- They must choose between two options:
  - **A) Surrender:** The opposing team gets 2 points and a new hand starts
  - **B) Play:** The hand continues with stakes set to 4 points (equivalent to accepting truco)
- **NO ONE can call truco** during this hand (truco calls are completely disabled)
- **Team Advantage:** The team at last hand can see their partner's cards (visibility advantage)

### When BOTH teams are at "Last Hand":
- The hand is played normally with initial stakes (2 points)
- **Truco and all raises are completely disabled**
- Whoever wins this hand wins the entire match
- **Iron Hand Rule** (configurable): If enabled, players cannot see their own cards until they are played

## Iron Hand Feature

Iron Hand is a special rule that can be enabled when both teams are at the last hand:

- **Card Visibility:** Players cannot see their own cards before playing them
- **Card Selection:** Players choose cards by index position (0, 1, 2) in their hand
- **Card Reveal:** Cards are only revealed when played in the trick area
- **Configuration:** Controlled by the `IronHandFlag` setting (default: disabled)

## Game Configuration

The following values can be configured for different Truco variants:

- **Victory Points:** Points needed to win the game (default: 12)
- **Minimum Stakes:** Starting stakes value for each hand (default: 2)  
- **Iron Hand Flag:** Enable/disable Iron Hand feature (default: false)
- **Stakes Progression:** The sequence of stake values (default: [2, 4, 8, 10, 12])

## State Transitions

The valid transitions are:

- `None` → `Truco` (any team calls Truco)
- `Truco` → `Accept` / `Surrender` / `Seis (Raise)` (only opposing team can respond)
- `Seis` → `Accept` / `Surrender` / `Nove (Raise)` (only opposing team can respond)
- `Nove` → `Accept` / `Surrender` / `Doze (Raise)` (only opposing team can respond)
- `Doze` → `Accept` / `Surrender` (only opposing team can respond)

## Call & Raise Flow

```mermaid
stateDiagram-v2
    [*] --> None
    None --> Truco: Any team calls Truco
    Truco --> Accepted: Opponent accepts (stakes=4)
    Truco --> Surrendered: Opponent surrenders (2 points to caller)
    Truco --> Seis: Opponent raises to Seis (stakes=8)
    Accepted --> Seis: Opponent raises to Seis (stakes=8)
    Seis --> Accepted: Opponent accepts (stakes=8)
    Seis --> Surrendered: Opponent surrenders (4 points to caller)
    Seis --> Nove: Opponent raises to Nove (stakes=10)
    Nove --> Accepted: Opponent accepts (stakes=10)
    Nove --> Surrendered: Opponent surrenders (8 points to caller)
    Nove --> Doze: Opponent raises to Doze (stakes=12)
    Doze --> Accepted: Opponent accepts (stakes=12)
    Doze --> Surrendered: Opponent surrenders (10 points to caller)
```

### Notes

- Only one call/raise can be in progress at a time.
- Stakes are updated immediately upon acceptance of a call/raise.
- Surrender events award points to the opposing team based on the previous stake level.
- Maximum stake is 12 (Doze), but this can be configured.
- After each raise, only the opposing team can respond with accept, surrender, or a further raise (if allowed).
- After a raise, the possibility to raise remains open for the opposing team until the end of the hand.
- At the start of a new hand, stakes reset to 2 and either team can call Truco.
- **Mão de 10:** When a team reaches 10 points, the hand starts in Truco state, only surrender or play (worth 4 points) is allowed, and further raises are disabled.
- **Both teams with 10 points:** Hand is played for 2 points, Truco and raises are disabled, and the winner of the hand wins the match.

---

### Exclusions
- The game does not include Envido or Flor, as they are not used in the Truco Mineiro variant.

## AI Player Behavior

The AI players implement sophisticated decision-making logic that simulates realistic Truco playing behavior, including strategic aggression, bluffing, and victory-awareness.

### AI Decision Framework

AI players use a context-aware decision system that considers:
- **Hand Strength**: Calculated based on card values and Truco rankings
- **Game Context**: Current scores, stakes, round winners, and hand number
- **Strategic Position**: Team position relative to victory/defeat
- **Psychological Factors**: Bluffing opportunities and opponent psychology

### Absolute Rules (Non-Negotiable Behavior)

#### Critical Victory/Defeat Prevention
1. **Never Surrender When Enemy Will Win**: If the opponent will achieve victory by winning the current stakes, AI will NEVER surrender
   - Will attempt to raise (even as a bluff) if possible
   - Will always accept if raise is not available
   - Applies desperate strategy when facing imminent defeat

2. **Never Raise When Own Victory is Assured**: If AI's team will win by accepting the current stakes, AI will NEVER raise
   - Avoids unnecessary risk when victory is guaranteed
   - Will only accept or surrender based on hand strength
   - Prioritizes securing the win over maximizing points

3. **Conservative Victory Protection**: When close to victory, AI becomes more conservative to avoid throwing away a winning position

### Strategic Rules (Threshold-Based with Randomness)

#### Aggression Modifiers
- **Behind in Score**: +10% aggression when opponent team is ahead
- **Won First Round**: +30% aggression bonus (doubled for psychological advantage)
- **Random Variation**: ±5% random variation in aggression levels

#### Dynamic Thresholds (with ±10% randomness)
- **Base Accept Threshold**: 30% hand strength
- **Base Raise Threshold**: 70% hand strength (85% at high stakes ≥8)
- **Base Truco Call Threshold**: 60% hand strength
- **Thresholds are lowered by aggression bonuses** (more aggressive = lower requirements)

### Bluffing System

The AI implements multiple bluffing scenarios to create unpredictable and realistic gameplay:

#### Bluffing Scenarios
1. **Prime Bluffing (Won First Round + Weak Hand)**: 40% base chance
   - Triggered when AI won first round but has weak cards (< 20% strength)
   - Exploits opponent's perception of strength from round win

2. **Desperation Bluffing (Behind + Weak-Medium Hand)**: 25% base chance
   - Activated when behind in score with 20-50% hand strength
   - Increases to 35% if 3+ points behind

3. **Positional Bluffing (Early Game + Medium Hand)**: 15% base chance
   - Used in first 2 hands with 40-60% hand strength
   - Establishes aggressive table image

4. **Random Bluffing (Any Situation)**: 7.5% base chance
   - Pure unpredictability factor
   - Prevents AI from being too predictable

#### Bluffing Applications
- **Truco Calls**: 70% chance when bluffing conditions are met
- **Raises**: 50% chance when bluffing conditions are met
- **Responses**: Can lead to accepting or raising with weak hands

### Hand Strength Calculation

Hand strength is calculated as: `(Sum of Card Strengths) / (Number of Cards × 14)`

Where card strengths follow Truco Mineiro rankings:
- Zap (4♣): 14 points
- Copas (7♥): 13 points  
- Espadão (A♠): 12 points
- Espadinha (7♦): 11 points
- 3s: 10 points
- 2s: 9 points
- Aces: 8 points
- Kings: 7 points
- And so on...

### Behavioral Constants

All AI behavior is controlled by configurable constants in `AIBehaviorConstants`:

```csharp
// Base thresholds
BASE_ACCEPT_THRESHOLD = 0.3
BASE_RAISE_THRESHOLD = 0.7
HIGH_STAKES_RAISE_THRESHOLD = 0.85
BASE_TRUCO_CALL_THRESHOLD = 0.6

// Aggression modifiers
BEHIND_SCORE_AGGRESSION_BONUS = 0.1
WON_FIRST_ROUND_AGGRESSION_BONUS = 0.3

// Bluffing parameters
BLUFF_BASE_CHANCE = 0.25
WEAK_HAND_THRESHOLD = 0.2
BLUFF_RANDOM_FACTOR = 0.3

// Randomness factors
THRESHOLD_RANDOM_VARIATION = 0.1
AGGRESSION_RANDOM_FACTOR = 0.05
```

### Examples of AI Behavior

#### Scenario 1: Enemy About to Win (11 points, stakes = 2)
- AI will NEVER surrender (absolute rule)
- Will attempt desperate raises even with weak hands
- 60-80% chance of aggressive actions depending on game context

#### Scenario 2: AI About to Win (11 points, stakes = 2)  
- AI will NEVER raise (absolute rule)
- Will only accept/surrender based on conservative hand strength threshold
- Prioritizes securing victory over point maximization

#### Scenario 3: Won First Round with Weak Hand
- 40% base chance to bluff with truco calls/raises
- Exploits psychological advantage from round win
- May lead to surprising aggressive plays with poor cards

#### Scenario 4: Behind in Score
- Increased aggression across all decisions
- More likely to call truco and make risky plays
- Desperation bluffing with medium-strength hands

### AI Card Selection Strategy

Beyond truco decisions, AI also implements sophisticated card selection:

1. **No Chance to Win**: Play weakest card to preserve stronger cards
2. **Partner Has Strongest**: Support partner or save cards based on remaining opponents
3. **Can Win Round**: Use smallest winning card to conserve strength
4. **First to Play**: Aggressive with strong hands, conservative with weak hands
5. **Positional Play**: Adapt strategy based on turn order and opponent actions

This AI system creates challenging, unpredictable opponents that provide an engaging single-player experience while maintaining the strategic depth and psychological elements that make Truco compelling.

## API Data Transfer Objects (DTOs)

The backend exposes the following DTOs (Data Transfer Objects) which define the contract between frontend and backend:

### Game Management DTOs

#### StartGameRequest
```csharp
public class StartGameRequest
{
    public string PlayerName { get; set; } = string.Empty;
}
```

#### StartGameResponse
```csharp
public class StartGameResponse
{
    public string GameId { get; set; } = string.Empty;
    public int PlayerSeat { get; set; }
    public List<TeamDto> Teams { get; set; } = new();
    public List<PlayerDto> Players { get; set; } = new();
    public List<CardDto> Hand { get; set; } = new();
    public List<PlayerHandDto> PlayerHands { get; set; } = new();
    public int DealerSeat { get; set; }
    public Dictionary<string, int> TeamScores { get; set; } = new();
    public int Stakes { get; set; }
    public int CurrentHand { get; set; }
    public List<ActionLogEntryDto> Actions { get; set; } = new();
}
```

### Card and Player DTOs

#### CardDto
```csharp
public class CardDto
{
    public string? Value { get; set; }  // null for hidden cards
    public string? Suit { get; set; }   // null for hidden cards
}
```

#### PlayerDto
```csharp
public class PlayerDto
{
    public int Seat { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public List<CardDto> Hand { get; set; } = new();
}
```

#### PlayerHandDto
```csharp
public class PlayerHandDto
{
    public int Seat { get; set; }
    public List<CardDto> Cards { get; set; } = new();
}
```

#### PlayedCardDto
```csharp
public class PlayedCardDto
{
    public int PlayerSeat { get; set; }
    public CardDto Card { get; set; } = new();
}
```

### Action DTOs

#### PlayCardRequestDto
```csharp
public class PlayCardRequestDto
{
    public string GameId { get; set; } = string.Empty;
    public int PlayerSeat { get; set; }      // 0-3
    public int CardIndex { get; set; }       // 0-based index
    public bool IsFold { get; set; } = false; // true to fold round
}
```

#### PlayCardResponseDto
```csharp
public class PlayCardResponseDto
{
    public GameStateDto GameState { get; set; } = new();
    public List<CardDto> Hand { get; set; } = new();
    public List<PlayerHandDto> PlayerHands { get; set; } = new();
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
}
```

#### ButtonPressRequest
```csharp
public class ButtonPressRequest
{
    public string GameId { get; set; } = string.Empty;
    public int PlayerSeat { get; set; }      // 0-3
    public string Action { get; set; } = string.Empty;  // "truco", "raise", "fold"
}
```

### Game State DTOs

#### GameStateDto
```csharp
public class GameStateDto
{    public List<PlayerDto> Players { get; set; } = new();
    public List<PlayedCardDto> PlayedCards { get; set; } = new();
    public int Stakes { get; set; }
    public string TrucoCallState { get; set; } = "None"; // None|Truco|Seis|Doze
    public int? LastTrucoCallerTeam { get; set; }
    public int? CanRaiseTeam { get; set; }
    public bool IsBothTeamsAt10 { get; set; }
    public int CurrentStakes { get; set; }
    public int CurrentHand { get; set; }
    public Dictionary<string, int> TeamScores { get; set; } = new();
    public string? TurnWinner { get; set; }
    public List<ActionLogEntryDto> ActionLog { get; set; } = new();
}
}
```

#### ActionLogEntryDto
```csharp
public class ActionLogEntryDto
{
    public string Type { get; set; } = string.Empty;
    public int? PlayerSeat { get; set; }
    public string? Card { get; set; }
    public string? Action { get; set; }
    public int? HandNumber { get; set; }
    public string? Winner { get; set; }
    public string? WinnerTeam { get; set; }
}
```

### DTO Usage Examples

#### Start a new game
```json
POST /api/game/start
{
  "playerName": "John"
}
```

#### Play a card
```json
POST /api/game/play-card
{
  "gameId": "abc123",
  "playerSeat": 0,
  "cardIndex": 0,
  "isFold": false
}
```

#### Fold current round
```json
POST /api/game/play-card
{
  "gameId": "abc123",
  "playerSeat": 0,
  "cardIndex": 0,
  "isFold": true
}
```

#### Call Truco
```json
POST /api/game/press-button
{
  "gameId": "abc123",
  "playerSeat": 0,
  "action": "truco"
}
```

#### Fold entire hand
```json
POST /api/game/press-button
{
  "gameId": "abc123",
  "playerSeat": 0,
  "action": "fold"
}
```

**All DTOs are serialized as JSON and use camelCase for property names when sent over the wire.**

## API Endpoints

The backend exposes the following RESTful API endpoints using event-driven architecture:

### Health Check
- **`GET /api/game/health`** — Returns API health status and version information.

### Game Management
- **`POST /api/game/start`** — Creates and starts a new game. Body: `{ "playerName": "string" }`
- **`GET /api/game/{gameId}?playerSeat={seat}`** — Returns current game state with player-specific card visibility.

### Game Actions
- **`POST /api/game/play-card`** — Plays a card or folds round. Body: `{ "gameId": "string", "playerSeat": 0, "cardIndex": 0, "isFold": false }`
- **`POST /api/game/press-button`** — Unified endpoint for Truco/Raise/Fold actions. Body: `{ "gameId": "string", "playerSeat": 0, "action": "truco|raise|fold" }`

### Endpoint Details

#### Health Check
```
GET /api/game/health
Response: { "status": "healthy", "timestamp": "2024-01-01T00:00:00Z", "service": "TrucoMineiro.API", "version": "1.0.0" }
```

#### Start Game
```
POST /api/game/start
Body: { "playerName": "John" }
Response: StartGameResponse with game state and player's cards
```

#### Get Game State
```
GET /api/game/{gameId}?playerSeat=0
Response: GameStateDto with appropriate card visibility based on requesting player
```

#### Play Card / Fold Round
```
POST /api/game/play-card
Body: 
{
  "gameId": "abc123",
  "playerSeat": 0,
  "cardIndex": 0,        // index of card to play (ignored if isFold=true)
  "isFold": false        // true to fold current round
}
Response: PlayCardResponseDto with updated game state
```

#### Button Press Actions (Unified)
```
POST /api/game/press-button
Body:
{
  "gameId": "abc123",
  "playerSeat": 0,
  "action": "truco"      // "truco", "raise", or "fold"
}
Response: GameStateDto with updated game state
```

### Action Types Explained

#### Play Card / Fold Round
- **Normal Play**: Send `{ "gameId": "abc123", "playerSeat": 0, "cardIndex": 0, "isFold": false }`
- **Fold Round**: Send `{ "gameId": "abc123", "playerSeat": 0, "cardIndex": 0, "isFold": true }` (gives up current round only)

#### Unified Button Press Actions
- **Truco**: `{ "gameId": "abc123", "playerSeat": 0, "action": "truco" }` — Initial Truco call
- **Raise**: `{ "gameId": "abc123", "playerSeat": 0, "action": "raise" }` — Counter-raise after opponent's Truco
- **Fold**: `{ "gameId": "abc123", "playerSeat": 0, "action": "fold" }` — Fold entire hand (all remaining rounds)

### Card Visibility Rules
- **Human Player**: Always sees their own cards with full details
- **AI Players**: Cards hidden (Value=null, Suit=null) unless DevMode is enabled
- **DevMode**: When enabled, all cards are visible for debugging purposes
- **Played Cards**: Always visible to all players once played

## Developer Instructions

### Prerequisites
- .NET 9.0 SDK or later
- Visual Studio 2022 or VS Code with C# extensions

### Quick Start (Development Mode)

#### Using the convenience script:
```powershell
# On Windows with PowerShell
.\start-dev.ps1

# Or with Command Prompt
start-dev.bat
```

This will start the API in development mode and open Swagger UI in your default browser.

### Building and Running Manually

#### Build the project
```powershell
dotnet build
```

#### Run the project in development mode
```powershell
dotnet run --project TrucoMineiro.API --launch-profile "TrucoMineiro.API.Development"
```

#### Access Swagger UI
Open your browser and navigate to: https://localhost:7120/

### Run unit tests
```powershell
dotnet test
```

### Docker support
To build and run using Docker:
```powershell
docker build -t truco-backend .
docker run -p 8080:80 truco-backend
```

## Architecture

### Event-Driven Game System

The application implements a modern event-driven architecture for all game interactions, providing reactive and scalable game flow management with proper separation of concerns.

#### Core Components

##### Event System
```
TrucoMineiro.API.Domain.Events/
├── GameEventBase.cs           # Base class for all game events
├── IEventHandler.cs           # Interface for event handlers
├── IEventPublisher.cs         # Interface for event publishing
└── GameEvents/
    ├── PlayerTurnStartedEvent.cs  # Triggered when a player's turn begins
    ├── CardPlayedEvent.cs         # Triggered when a card is played
    ├── TrucoRaiseEvent.cs         # Triggered when Truco/Raise is called
    ├── SurrenderHandEvent.cs      # Triggered when a player surrenders hand
    └── RoundCompletedEvent.cs     # Triggered when a round finishes
```

##### Event Handlers
```
TrucoMineiro.API.Domain.EventHandlers/
├── AIPlayerEventHandler.cs         # Handles AI decision making and actions
├── GameFlowEventHandler.cs         # Manages card play and truco events
├── HandCompletionEventHandler.cs   # Handles hand surrender and completion
└── ActionLogEventHandler.cs        # Creates action log entries for frontend
```

#### Event Flow Architecture

```mermaid
graph TD    A[Frontend Button Press] --> B{Action Type}
    B -->|Play Card| C[PlayCardRequest with IsFold flag]
    B -->|Truco/Raise| D[TrucoRaiseEvent]
    B -->|Surrender Hand| E[SurrenderHandEvent]
    
    C --> F[Card replaced with FOLD card if IsFold=true]
    F --> G[CardPlayedEvent Published]
    D --> H[TrucoRaiseEventHandler]
    E --> I[HandCompletionEventHandler]
    
    G --> J[Multiple Event Handlers]
    H --> J
    I --> J
      J --> K[ActionLogEventHandler - Creates UI entries]
    J --> L[GameFlowEventHandler - Card play logic]
    J --> M[AIPlayerEventHandler - AI responses]
    
    I --> N[HandCompletionEventHandler - Hand termination]
    
    L --> O{Game State Check}
    O -->|Continue| P[Next PlayerTurnStartedEvent]
    O -->|Round Complete| Q[RoundCompletedEvent]
    P --> R[AI or Human Turn]
    Q --> S[Hand Complete Check]
```

#### Key Features

1. **Unified Button Press Handling**: All frontend actions (card play, truco/raise, fold) flow through structured event system
2. **Smart Card Replacement**: PlayCardRequest with IsFold flag replaces card with FOLD card in PlayedCard list
3. **Event-Driven AI**: AI players respond to events rather than synchronous calls
4. **Realistic Timing**: AI players have thinking delays (500-2000ms) for better user experience
5. **Loose Coupling**: Event handlers are decoupled from direct service calls
6. **Action Log Integration**: ActionLogEventHandler creates all frontend display entries from events
7. **Extensibility**: Easy to add new event handlers for additional game features

#### Button Press Event Specifications

##### Card Play with Fold Option
- **Frontend**: Sends `PlayCardRequest` with `IsFold: true` instead of card selection
- **Backend**: Replaces player's card with FOLD card in `PlayedCard` list
- **Event**: `CardPlayedEvent` published (fold status determined by checking if card is FOLD)
- **Result**: Player gives up current round, opponent wins automatically

##### Truco/Raise (Unified Action)
- **Logic**: Since you can only raise after opponent's truco/raise, these are the same button
- **Frontend**: Single "Truco/Raise" button that adapts based on game state
- **Backend**: `TrucoRaiseEvent` published with current stakes information
- **Event**: Opposing team must respond (accept, raise further, or fold entire hand)

##### Fold All Cards
- **Purpose**: Give up all remaining cards in the hand (not just current round)
- **Frontend**: "Surrender Hand" button (separate from round fold)
- **Backend**: `SurrenderHandEvent` published, hand immediately terminated
- **Result**: Opponent wins the hand immediately, gets points based on current stakes

#### Service Architecture

##### Core Interfaces
- **`IAIPlayerService`**: AI decision making and card selection logic
- **`IGameStateManager`**: Game state lifecycle and management
- **`IGameFlowService`**: Game flow control and turn management
- **`IHandResolutionService`**: Card ranking and round winner determination
- **`IGameRepository`**: Game state persistence and retrieval
- **`IEventPublisher`**: Event publishing and distribution system

##### Event Handler Architecture
- **`ActionLogEventHandler`**: Creates `ActionLogEntry` records for frontend display from all game events
- **`GameFlowEventHandler`**: Manages card play events and truco/raise responses
- **`HandCompletionEventHandler`**: Handles hand surrender events, score updates, and game completion
- **`AIPlayerEventHandler`**: Handles AI player decision making and automatic actions

##### Request/Response Flow Validation

###### PlayCardRequest with IsFold
```csharp
// Frontend sends:
{
  "playerId": "player1",
  "cardIndex": 0,        // ignored when IsFold=true
  "isFold": true         // indicates fold action
}

// Backend validation:
1. If IsFold=true, replace card with new Card { Value="FOLD", Suit="NONE" }
2. Add to PlayedCards list with FOLD card
3. Publish CardPlayedEvent (IsFold property can be removed since card inspection determines fold)
4. ActionLogEventHandler creates appropriate UI entry
5. GameFlowEventHandler determines round winner (opponent wins automatically)
```

###### TrucoRaiseEvent Structure
```csharp
public class TrucoRaiseEvent : GameEventBase
{
    public string PlayerId { get; set; }
    public int CurrentStakes { get; set; }
    public int NewStakes { get; set; }
    public bool IsInitialTruco { get; set; }  // true for first truco, false for raise
}
```

###### SurrenderHandEvent Structure  
```csharp
public class SurrenderHandEvent : GameEventBase
{
    public Player Player { get; set; }
    public int HandNumber { get; set; }
    public int CurrentStake { get; set; }
    public string WinningTeam { get; set; }
    public GameState GameState { get; set; }
}
}
```

##### Dependency Injection
All event handlers and services are registered in the DI container (`Program.cs`) with proper scoping:
```csharp
// Event Handlers
services.AddScoped<IEventHandler<PlayerTurnStartedEvent>, AIPlayerEventHandler>();
services.AddScoped<IEventHandler<CardPlayedEvent>, GameFlowEventHandler>();
services.AddScoped<IEventHandler<CardPlayedEvent>, ActionLogEventHandler>();
services.AddScoped<IEventHandler<TrucoRaiseEvent>, GameFlowEventHandler>();
services.AddScoped<IEventHandler<SurrenderHandEvent>, HandCompletionEventHandler>();

// Event Publishing
services.AddScoped<IEventPublisher, InMemoryEventPublisher>();

// Core Services
services.AddScoped<IAIPlayerService, AIPlayerService>();
services.AddScoped<IGameStateManager, GameStateManager>();
services.AddScoped<IHandResolutionService, HandResolutionService>();
```

#### Testing Strategy

The event-driven system includes comprehensive integration tests:
- **`EventDrivenAIPlayerTests`**: Tests AI player event handling with realistic game scenarios
- **`ActionLogEventHandlerTests`**: Validates proper ActionLog entry creation from events
- **`ButtonPressEventTests`**: Tests Truco/Raise and Fold event handling workflows
- **Mock Implementations**: Complete test doubles for all interfaces
- **Event Validation**: Validates proper event publishing and chaining behavior
- **Integration Tests**: End-to-end testing of button press → event → response workflows

#### Implementation Requirements

##### Phase 2: Structured Event Definitions (Current)
1. **Remove IsFold property from CardPlayedEvent** - determine fold by card inspection
2. **Create TrucoRaiseEvent and SurrenderHandEvent classes** with proper structure
3. **Update PlayCardRequest handling** to replace card with FOLD card when IsFold=true
4. **Implement event handlers** for TrucoRaiseEvent and SurrenderHandEvent
5. **Add comprehensive tests** for all button press scenarios

##### Phase 3: Event-Driven AI Integration (Next)
1. **Expand AI decision making** to handle Truco/Raise/Fold responses via events
2. **Add AI strategy patterns** for different game situations
3. **Implement AI response delays** for realistic gameplay experience

### Domain Models

#### GameState
Central domain model containing:
- Player information and hands
- Current game state (round, hand, scores)
- Played cards and action history
- Team scores and game completion status

#### Player
Represents game participants with:
- Identity (name, seat, team)
- Current hand of cards
- AI/Human designation
- Active status

#### Card & PlayedCard
Card representation with suit and value, and tracking of played cards with player association.

## Frontend Integration

This backend is designed to work with the Truco Mineiro frontend project. The event-driven architecture ensures smooth real-time game flow and responsive AI interactions. For more details, see the frontend documentation or contact the frontend team.