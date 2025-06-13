# Project Context for Truco Mineiro

## Project Overview

Truco Mineiro is a C# ASP.NET Core implementation of the Brazilian card game Truco, featuring:
- **Real-time multiplayer gameplay** with AI opponents
- **Event-driven architecture** for game state management
- **RESTful API** for frontend integration
- **Comprehensive rule engine** including special cases like "Mão de 10"

## Current Architecture

### **Domain-Driven Design**
```
TrucoMineiro.API/
├── Domain/
│   ├── Models/          # Core game entities
│   ├── Events/          # Domain events
│   ├── Services/        # Business logic
│   ├── Interfaces/      # Service contracts
│   └── StateMachine/    # Game flow management
├── Controllers/         # API endpoints
├── DTOs/               # Data transfer objects
└── Constants/          # Application constants
```

### **Key Components**

#### **GameState (Aggregate Root)**
- Single source of truth for game state
- Tracks players, scores, cards, and truco progression
- Uses proper enums (`GameStatus`, `TrucoCallState`) with string compatibility

#### **Event-Driven State Machine**
- **Commands**: Represent player actions (CallTrucoOrRaiseCommand, PlayCardCommand)
- **Events**: Represent things that happened (TrucoCalledEvent, CardPlayedEvent)
- **Handlers**: Process events and update state

#### **TrucoRulesEngine**
- Validates game actions
- Implements Brazilian Truco rules
- Handles special cases and edge conditions

## Business Rules

### **Truco Stakes Progression**
1. **Base Hand**: 2 points (updated from original 1 point)
2. **Truco Call**: 4 points
3. **Seis Raise**: 8 points  
4. **Doze Raise**: 12 points (maximum)

### **Team Alternation Rules**
- Teams cannot call/raise consecutively
- Only the opposing team can accept/surrender/counter-raise

### **"Mão de 10" Special Rule**
- When both teams have 10 points, truco calls are disabled
- Hand automatically worth 4 points (as if Truco was called)

### **Game Flow**
1. **Card Phase**: Players play cards in turn order
2. **Truco Phase**: Can be triggered during card play
3. **Resolution**: Hand winner determined, points awarded
4. **Next Hand**: New cards dealt, play continues

## State Management

### **Current State Model**
```csharp
public class GameState 
{
    // Core Properties
    public GameStatus Status { get; set; }           // Enum-based
    public string GameStatus => Status.ToString();   // API compatibility
    
    // Truco State (New Model)
    public TrucoCallState TrucoCallState { get; set; } = TrucoCallState.None;
    public int? LastTrucoCallerTeam { get; set; }
    public int? CanRaiseTeam { get; set; }
    public bool IsBothTeamsAt10 { get; set; }
    public int Stakes { get; set; } = 2; // Current hand value
    
    // Legacy Properties (REMOVED)
    // ❌ IsTrucoCalled, IsRaiseEnabled, TrucoLevel, WaitingForTrucoResponse
}
```

### **Command/Event Pattern**
```csharp
// Commands (Intent)
CallTrucoOrRaiseCommand -> TrucoOrRaiseCalledEvent
AcceptTrucoCommand -> TrucoAcceptedEvent  
SurrenderTrucoCommand -> TrucoSurrenderedEvent

// Processing Flow
API Controller -> GameStateMachine -> Domain Events -> Event Handlers
```

## API Design

### **Button Press System**
Unified endpoint for all player actions:
```
POST /api/truco-game/button-press
{
    "gameId": "abc123",
    "playerSeat": 0,
    "action": "CallTrucoOrRaise" | "AcceptTruco" | "SurrenderTruco"
}
```

### **Game State Response**
```json
{
    "gameId": "abc123",
    "gameStatus": "active",
    "trucoCallState": "Truco",
    "stakes": 4,
    "lastTrucoCallerTeam": 0,
    "canRaiseTeam": 1,
    "isBothTeamsAt10": false
}
```

## Frontend Integration Points

### **Available Actions**
Frontend receives available actions based on game state:
- `"play-card"` - Normal card play
- `"call-truco-or-raise"` - Call Truco or raise stakes
- `"accept-truco"` - Accept opponent's truco call
- `"surrender-truco"` - Surrender to truco call
- `"fold"` - Surrender the hand

### **Real-time Updates**
- Game state changes trigger events
- Frontend should listen for state updates
- Button availability changes dynamically

## Testing Strategy

### **Unit Tests**
- **GameStateMachine**: Command processing and validation
- **TrucoRulesEngine**: Rule validation and edge cases
- **Events**: Proper event publication and data

### **Integration Tests**
- **API Endpoints**: Full request/response cycles
- **Game Flow**: Complete game scenarios
- **AI Integration**: Automated player behavior

## Configuration

### **Game Constants**
All configurable values in `TrucoConstants.cs`:
- Point values and progression
- Player limits and timing
- Command and action names

### **Environment Settings**
- Development: Enhanced logging, test data
- Production: Optimized performance, security

## Future Considerations

### **Planned Features**
- Multiplayer lobbies
- Tournament mode
- Advanced AI difficulty levels
- Game replay system

### **Technical Debt Items**
- Complete legacy code removal
- Performance optimization for large player counts
- Enhanced error handling and logging
- Comprehensive integration test coverage

## Dependencies

### **Core Framework**
- .NET 9.0
- ASP.NET Core
- Entity Framework Core (if using database)

### **Key Libraries**
- Moq (testing)
- xUnit (testing framework)
- System.Text.Json (serialization)

### **Development Tools**
- Visual Studio Code
- Git for version control
- GitHub Copilot for AI assistance
