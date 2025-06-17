# Dynamic Stakes Refactor & Iron Hand Implementation Plan

## 📋 Current State Analysis

### ✅ README Documentation Review
Based on the current README.md, the documented rules are:

**Stakes Progression (Current Documentation):**
| Call/Raise | Points at Stake |
|------------|----------------|
| Initial    | 2              |
| Truco      | 4              |
| Seis       | 8              |
| **Missing**| **10**         |
| Doze       | 12             |

**"Mão de 10" Rules (Current Documentation):**
- When team reaches 10 points → automatic truco state
- Team at 10 must: surrender (2 pts to opponent) OR play (4 pts)
- Further raises (Seis, Doze) are **disabled**
- Both teams at 10 → normal hand (2 pts), no truco allowed

### ❌ Identified Issues

1. **Missing "Nove" (10 points) in progression** - README shows 2→4→8→12, missing the 10 step
2. **Hardcoded "Mão de 10" logic** - Should be dynamic "last hand" calculation
3. **No Iron Hand feature** - Missing from documentation and implementation
4. **No partner card visibility** - Missing feature for team at last hand

## 🎯 Goals & Requirements

### 📚 Rule Corrections & Clarifications

#### ✅ Correct Stakes Progression
```
2 (Initial) → 4 (Truco) → 8 (Seis) → 10 (Nove) → 12 (Doze/Maximum)
```

#### ✅ Dynamic "Last Hand" Logic
- Replace hardcoded "10 points" with calculation: `teamScore + minimumStakes >= victoryPoints`
- Default: `10 + 2 >= 12` (true) = last hand
- Flexible for future variants (e.g., Truco Paulista with different victory points)

#### ✅ Corrected Last Hand Behavior  
**When ONE team at last hand:**
- ❌ **OLD:** "Only that team can call truco"
- ✅ **NEW:** Team is automatically in truco-like state, must choose:
  - A) Surrender → opponent gets 2 points, next hand starts
  - B) Play → stakes set to 4 points, hand continues normally
- ✅ **NO ONE** can call truco when any team is at last hand
- ✅ **Team at last hand sees partner's cards** (visibility advantage)

**When BOTH teams at last hand:**
- ✅ Normal hand (2 points)
- ✅ No truco calls allowed
- ✅ **NEW: Iron Hand Rule** (if enabled):
  - Players cannot see their own cards before playing
  - Cards selected by index (0, 1, 2)
  - Cards revealed only when played

#### ✅ New Features
1. **Iron Hand Flag** - Global configuration like DevMode
2. **Partner Card Visibility** - Team at last hand sees both player + partner cards
3. **Dynamic Victory Points** - Configurable for different truco variants

## 🔧 Technical Implementation Plan

### 📦 Phase 1: Foundation Refactor (Days 1-2)

#### 1.1 Update TrucoConstants.cs
- ✅ Add stakes progression array: `[2, 4, 8, 10, 12]`
- ✅ Add Iron Hand flag
- ✅ Ensure single source of truth for victory/stakes values

#### 1.2 Simplify TrucoCallState Enum
```csharp
public enum TrucoCallState 
{
    None = 0,           // No pending truco call
    PendingResponse = 1 // Waiting for response to truco/raise
}
```

#### 1.3 Extend ITrucoRulesEngine Interface
- ✅ Add new "last hand" methods
- ✅ Add stakes progression helpers
- ✅ Keep existing methods for backward compatibility

### 📝 Phase 2: Core Logic Refactor (Days 2-3)

#### 2.1 TrucoRulesEngine.cs
**Add new methods:**
```csharp
bool IsLastHand(int teamScore);
bool IsOneTeamAtLastHand(GameState game);
bool AreBothTeamsAtLastHand(GameState game);
Team? GetTeamAtLastHand(GameState game);
int GetNextStakes(int currentStakes);
bool IsMaximumStakes(int stakes);
```

**Update existing methods:**
- Replace hardcoded values with `TrucoConstants` references
- Use stakes progression array for calculations

#### 2.2 MappingService.cs (Available Actions)
**Replace current logic:**
```csharp
// OLD: Hardcoded checks
bool areBothTeamsAt10 = _trucoRulesEngine.AreBothTeamsAt10(gameState);
if (gameState.Stakes < TrucoConstants.Stakes.Maximum)

// NEW: Dynamic checks  
bool areBothTeamsAtLastHand = _trucoRulesEngine.AreBothTeamsAtLastHand(gameState);
if (!_trucoRulesEngine.IsMaximumStakes(gameState.Stakes))
```

#### 2.3 GameStateMachine.cs
- Use `TrucoConstants.StakesProgression` for progression logic
- Replace `TrucoCallState.Doze` with `TrucoCallState.PendingResponse`
- Use `GetNextStakes()` for raise calculations

### 🎴 Phase 3: New Features (Days 3-4)

#### 3.1 Iron Hand Implementation
**Configuration:**
```csharp
// TrucoConstants.cs
public const bool IronHandFlag = false; // Global setting
```

**Logic:**
```csharp
// MappingService.cs - Card visibility
bool hideCards = TrucoConstants.IronHandFlag && 
                _trucoRulesEngine.AreBothTeamsAtLastHand(gameState) &&
                !player.IsAI; // AIs still need card data

if (hideCards && !showAllHands) 
{
    playerDto.Hand = player.Hand.Select(c => CardDto.CreateHidden()).ToList();
}
```

#### 3.2 Partner Card Visibility
**When:** One team at last hand
**Logic:**
```csharp
// MappingService.cs
bool showPartnerCards = _trucoRulesEngine.IsOneTeamAtLastHand(gameState) && 
                       _trucoRulesEngine.GetTeamAtLastHand(gameState) == player.Team;

if (showPartnerCards) 
{
    var partner = gameState.Players.FirstOrDefault(p => 
        p.Team == player.Team && p.Seat != player.Seat);
    
    if (partner != null) 
    {
        // Make partner's cards visible in player's hand view
        playerDto.PartnerHand = partner.Hand.Select(c => MapCardToDto(c, true)).ToList();
    }
}
```

#### 3.3 Last Hand Behavior  
**Available Actions:**
```csharp
if (_trucoRulesEngine.IsOneTeamAtLastHand(gameState)) 
{
    var teamAtLastHand = _trucoRulesEngine.GetTeamAtLastHand(gameState);
    if (currentPlayer.Team == teamAtLastHand) 
    {
        actions.Add("surrender-last-hand"); // 2 points to opponent
        actions.Add("play-last-hand");      // Stakes = 4, continue
    }
    // Block all truco calls when any team at last hand
}
```

### 📚 Phase 4: Documentation Updates (Day 4)

#### 4.1 Update README.md
**Add missing progression step:**
```markdown
## Stakes Progression

| Call/Raise | Points at Stake |
|------------|----------------|
| Initial    | 2              |
| Truco      | 4              |
| Seis       | 8              |
| **Nove**   | **10**         |
| Doze       | 12             |
```

**Correct "Mão de 10" rules:**
```markdown
## Special Rule: "Last Hand" (formerly "Mão de 10")

- When a team reaches the last hand threshold (default: 10 points), special rules apply:
    - The team at last hand is automatically in a truco-like state
    - They must decide: surrender (2 points to opponent) or play (stakes = 4 points)
    - **NO ONE** can call truco when any team is at last hand
    - **Team at last hand can see partner's cards** (visibility advantage)

- **If both teams are at last hand:**
    - Hand is played normally (stakes = 2), but truco is **disabled**
    - **Iron Hand Rule** (if enabled): Players cannot see their own cards until played
```

**Add new features:**
```markdown
## Iron Hand Feature

When both teams are at the last hand and Iron Hand is enabled:
- Players cannot see their own cards before playing
- Cards are selected by index (0, 1, 2) 
- Cards are revealed only when played in the trick area

## Configuration

- `IronHandFlag`: Enable/disable Iron Hand feature (default: false)
- `VictoryPoints`: Points needed to win (default: 12)
- `MinimumStakes`: Starting stakes value (default: 2)
```

### 🧪 Phase 5: Test Updates (Day 5)

#### 5.1 Update Test Files
**Rename files:**
- `MaoDe10SpecialRulesTests.cs` → `LastHandSpecialRulesTests.cs`
- `MaoDe10AvailableActionsTests.cs` → `LastHandAvailableActionsTests.cs`

**Replace hardcoded values:**
```csharp
// OLD
game.Team1Score = 10;
Assert.True(engine.IsOneTeamAt10(game));

// NEW  
game.Team1Score = TrucoConstants.Game.VictoryPoints - TrucoConstants.Stakes.Initial;
Assert.True(engine.IsOneTeamAtLastHand(game));
```

#### 5.2 Add New Feature Tests
- Iron Hand visibility tests
- Partner card visibility tests  
- Last hand behavior tests
- Stakes progression tests

## 📋 Implementation Steps

### Day 1: Foundation
1. ✅ Update `TrucoConstants.cs` with progression array and flags
2. ✅ Simplify `TrucoCallState` enum
3. ✅ Extend `ITrucoRulesEngine` interface with new methods

### Day 2: Core Refactor
4. ✅ Implement new methods in `TrucoRulesEngine.cs`
5. ✅ Update `MappingService.cs` available actions logic
6. ✅ Refactor `GameStateMachine.cs` command processing
7. ✅ Replace hardcoded values throughout codebase

### Day 3: New Features - Part 1
8. ✅ Implement Iron Hand feature
9. ✅ Add partner card visibility logic
10. ✅ Implement last hand behavior (surrender/play choice)

### Day 4: New Features - Part 2 & Documentation
11. ✅ Add new available actions for last hand
12. ✅ Update README.md with corrected rules
13. ✅ Add feature documentation

### Day 5: Testing & Validation
14. ✅ Update all test files with dynamic values
15. ✅ Add comprehensive tests for new features
16. ✅ Ensure all tests pass (117+ tests)
17. ✅ Manual testing and validation

## ⚠️ Risk Assessment

### High Risk
- **Stakes progression logic** - Core game mechanic, many dependencies
- **Available actions changes** - Affects game flow and UI

### Medium Risk  
- **TrucoCallState simplification** - May affect state tracking
- **Test file updates** - Large number of tests to modify

### Low Risk
- **Iron Hand feature** - New feature, can be disabled
- **Documentation updates** - No code impact

### Mitigation
- ✅ Incremental commits with frequent testing
- ✅ Keep old methods temporarily for backward compatibility
- ✅ Feature flags for new functionality
- ✅ Comprehensive test coverage

## 🎯 Success Criteria

### Functional
- ✅ Correct stakes progression: 2→4→8→10→12
- ✅ Dynamic last hand calculation working
- ✅ Last hand behavior: team must choose surrender/play
- ✅ No truco calls when any team at last hand
- ✅ Iron Hand: cards hidden when both teams at last hand
- ✅ Partner visibility: team at last hand sees partner's cards

### Technical
- ✅ All hardcoded values replaced with constants
- ✅ Single source of truth for game rules
- ✅ Clean, maintainable code structure
- ✅ All tests passing (117+ tests)
- ✅ Backward compatibility maintained during transition

### Documentation
- ✅ README.md updated with correct rules
- ✅ New features documented
- ✅ Stakes progression table corrected
- ✅ API documentation updated if needed

---

**Ready to proceed with Day 1: Foundation setup?**
