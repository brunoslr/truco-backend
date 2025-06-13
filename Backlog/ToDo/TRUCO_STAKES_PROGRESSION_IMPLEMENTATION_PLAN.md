# Truco Mineiro – Stakes Progression Implementation Plan

## Overview

This plan implements the complete stakes progression and call flow system for Truco Mineiro, including the special "Mão de 10" rules and proper team alternation logic.

## 1. Domain Model & State

**Remove from GameState:**
- `IsTrucoCalled` 
- `IsRaiseEnabled`

**Add to GameState:**
```csharp
public enum TrucoCallState
{
    None,    // No truco in progress, stakes = 2
    Truco,   // Truco called, waiting for response  
    Seis,    // Seis called, waiting for response
    Doze     // Doze called, waiting for response
}

// New properties
public TrucoCallState TrucoCallState { get; set; } = TrucoCallState.None;
public int LastTrucoCallerTeam { get; set; } = -1;     // -1 = no previous caller
public int CurrentStakes { get; set; } = 2;             // Current hand value
public int? CanRaiseTeam { get; set; } = null;         // Team that can raise (null = either can call truco)
public bool IsBothTeamsAt10 { get; set; } = false;     // Special case: disable all truco
```

## 2. TrucoEngineRules

**Core validation methods:**
- `CanCallTrucoOrRaise(gameState, playerId)` - validates escalation calls
- `CanAcceptTruco(gameState, playerId)` - validates acceptance
- `CanSurrenderTruco(gameState, playerId)` - validates surrender
- `GetNextStakeLevel(currentTrucoState)` - returns progression (2→4→8→12)
- `IsMaoDe10Scenario(teamScores)` - detects special rules
- `GetAllowedActions(gameState, playerId)` - returns available buttons
- `ResetTrucoStateForNewHand(gameState)` - resets to None, stakes=2

**Stake progression:**
```csharp
private static readonly Dictionary<TrucoCallState, int> NextStakes = new()
{
    { TrucoCallState.None, 4 },    // None → Truco = 4 points
    { TrucoCallState.Truco, 8 },   // Truco → Seis = 8 points  
    { TrucoCallState.Seis, 12 }    // Seis → Doze = 12 points
};
```

**Special rules validation:**
- Block all truco actions when `IsBothTeamsAt10 = true`
- In "Mão de 10" (one team has 10): start hand with `TrucoCallState.Truco`, disable raises

## 3. Event-Driven Flow

**Events:**
```csharp
TrucoOrRaiseCalledEvent 
{ 
    CallerTeam,
    CallType,          // "Truco", "Seis", "Doze" 
    PreviousStakes,    // Stakes before call
    NewPotentialStakes // Stakes if accepted
}

TrucoAcceptedEvent 
{ 
    AcceptingTeam,
    ConfirmedStakes,   // Stakes are now official
    CanRaiseTeam       // Which team can raise next (opposing team)
}

TrucoSurrenderedEvent
{ 
    SurrenderingTeam, 
    PointsAwarded     // Points given to opposing team
}
```

**Event Handlers:**
- `TrucoOrRaiseCalledEvent` → Update state, set waiting for response
- `TrucoAcceptedEvent` → Update stakes immediately, set raise permissions  
- `TrucoSurrenderedEvent` → Award points from current stakes, end hand
- `HandStartedEvent` → Reset truco state, check for "Mão de 10"

## 4. API Endpoints

**Use existing button press system:**
```csharp
POST /game/{id}/press-button
{
    "action": "CallTrucoOrRaise" | "AcceptTruco" | "SurrenderTruco"
}
```

**Enhanced state response:**
```csharp
GET /game/{id}/state
// Add to GameStateDto:
// - trucoCallState  
// - currentStakes
// - canRaiseTeam
// - isMaoDe10
// - isBothTeamsAt10
// - lastTrucoCallerTeam
```

## 5. Special Rules Implementation

### **"Mão de 10" Logic:**

**Case 1: One team has 10 points**
```csharp
// On HandStartedEvent
if (oneTeamHas10Points && !bothTeamsHave10Points)
{
    gameState.TrucoCallState = TrucoCallState.Truco;
    gameState.CurrentStakes = 4;
    gameState.LastTrucoCallerTeam = teamWith10Points;
    gameState.CanRaiseTeam = null; // No raises allowed
    // Opposing team can only Accept or SurrenderTruco
}
```

**Case 2: Both teams have 10 points**
```csharp
// On HandStartedEvent  
if (bothTeamsHave10Points)
{
    gameState.IsBothTeamsAt10 = true;
    gameState.CurrentStakes = 2;
    // All truco actions disabled
    // Winner of hand wins match
}
```

## 6. Team Decision Logic (Simplified)

**Current implementation:**
- Any player's decision applies to entire team
- No consensus logic needed

**Next Steps (Future):**
- Define and implement AI behavior
- Add team consensus for AI-only teams
- Human player overrides for mixed teams

## 7. Hand/Match End Integration

**Surrender during truco:**
- Award `CurrentStakes` points to opposing team
- Trigger `TrucoSurrenderedEvent` 
- End current hand

**Normal hand completion:**
- Award `CurrentStakes` points to winning team
- Check for match end (12+ points)

## 8. State Reset Logic

**New hand starts:**
```csharp
// In HandStartedEvent handler
gameState.TrucoCallState = TrucoCallState.None;
gameState.CurrentStakes = 2;
gameState.LastTrucoCallerTeam = -1;
gameState.CanRaiseTeam = null;
gameState.IsBothTeamsAt10 = CheckBothTeamsAt10(teamScores);

// Then check for Mão de 10 scenarios
```

## Implementation Strategy

### **Phase-by-Phase Approach**
- Complete each phase fully before moving to the next
- Validate each phase with unit tests and manual testing
- Commit changes after successful phase validation
- No parallel development across phases to maintain stability

### **Change Management Process**
To avoid implementation degradation and circular changes:

1. **Document Change Requests**: Before implementing any mid-stream changes, document:
   - What needs to change and why
   - Impact on current phase and future phases
   - Alternative approaches considered

2. **Pause and Assess**: Stop current implementation when significant changes are requested
   - Evaluate if change improves overall design
   - Consider if change should be deferred to future iteration
   - Assess complexity vs. benefit trade-off

3. **Version Control Strategy**: 
   - Work directly on main branch (solo project with AI assistance)
   - Commit after each successful phase validation
   - Use descriptive commit messages for easy rollback if needed

4. **Change Approval Criteria**:
   - Change must solve a clear problem
   - Change must not break existing functionality
   - Change must align with overall architecture
   - Change must not significantly increase complexity

5. **Implementation Rule**: If a change requires more than 30% rework of a completed phase, defer it to a future improvement cycle

---

## Implementation Phases

### **Phase 1: Core Domain**
1. Update `GameState` with new properties
2. Implement `TrucoEngineRules` methods
3. Create/update truco events

### **Phase 2: State Machine**
1. Add event handlers to `GameStateMachine`
2. Implement state transitions
3. Add hand reset logic

### **Phase 3: API Integration**  
1. Update `ButtonPressActions` with truco actions
2. Enhance `GameStateDto` responses
3. Update validation logic

### **Phase 4: Special Rules**
1. Implement "Mão de 10" scenarios
2. Add comprehensive validation
3. Integration testing

### **Phase 5: UI Updates**
1. Frontend truco state display
2. Button availability logic
3. Stakes and special rule indicators

### **Phase 6: Testing & Polish**
1. Unit tests for all scenarios
2. End-to-end testing
3. Performance validation

---

## Next Steps to Define (Future Improvements)
- **AI Behavior:** Implement smart truco calling/responding for AI players
- **Team Consensus:** Complex decision logic for AI-only teams  
- **State Persistence:** Reconnection and recovery during truco calls
- **Advanced UI:** Animations, sound effects, better feedback

---

## Key Success Criteria
- ✅ All README rules implemented correctly
- ✅ Proper stake progression and reset
- ✅ "Mão de 10" scenarios work perfectly
- ✅ Simplified team decision logic
- ✅ `SurrenderTruco` action properly integrated
- ✅ Extensible for future AI improvements

## Dependencies
- Existing event-driven architecture
- Current button press system
- GameState and DTO mappings
- TrucoEngineRules foundation

## Validation Requirements
- All state transitions must follow README rules exactly
- Stakes progression: 2→4→8→12
- Team alternation properly enforced
- Special cases (Mão de 10) handled correctly
- Existing gameplay functionality preserved
