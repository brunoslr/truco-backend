# Dynamic Stakes & Iron Hand Implementation Plan

## 🎯 Objectives

### Primary Goals
1. **Remove hardcoded stakes values** - Replace with dynamic progression system
2. **Replace "Mão de 10" logic** with `IsLastHand()` calculation  
3. **Add Iron Hand feature** - Hide cards when both teams at last hand
4. **Add partner card visibility** - Team at last hand sees partner's cards
5. **Implement last hand behavior** - Automatic truco-like state for team at last hand

### Design Principles
- ✅ Single source of truth for victory points and stakes
- ✅ Dynamic progression: `[2, 4, 8, 10, 12]` (Truco Mineiro specific)
- ✅ No hardcoded values scattered in code
- ✅ Flexible foundation for other truco variants

## 📦 Phase 1: Foundation Refactor

### 1.1 Update TrucoConstants.cs
```csharp
public static class TrucoConstants
{
    // Victory and stakes (single source of truth)
    public const int VictoryPoints = 12;
    public const int MinimumStakes = 2;
    public const int MaximumStakes = VictoryPoints; // Same value
    
    // Truco Mineiro progression: 2 → 4 → 8 → 10 → 12
    public static readonly int[] StakesProgression = { 2, 4, 8, 10, 12 };
    
    // Game configuration flags
    public const bool IronHandFlag = false; // Like DevMode
    
    // Helper methods
    public static bool IsLastHand(int teamScore) 
        => teamScore + MinimumStakes >= VictoryPoints;
    
    public static int GetNextStakes(int currentStakes) 
    {
        var index = Array.IndexOf(StakesProgression, currentStakes);
        return index >= 0 && index < StakesProgression.Length - 1 
            ? StakesProgression[index + 1] 
            : currentStakes;
    }
    
    public static bool IsMaximumStakes(int stakes) 
        => stakes >= MaximumStakes;
}
```

### 1.2 Simplify TrucoCallState Enum
**Decision:** Keep simplified version for state tracking
```csharp
public enum TrucoCallState 
{
    None = 0,           // No pending truco call
    PendingResponse = 1 // Truco/raise called, waiting for response
}
```

**Alternative:** Remove enum entirely and use `bool HasPendingTrucoCall`
- **Pros:** Simpler, no enum to maintain
- **Cons:** Lose semantic meaning for UI/logging

**Recommendation:** Start with simplified enum, can remove later if not needed.

## 📝 Phase 2: Core Logic Refactor

### 2.1 TrucoRulesEngine.cs
**Replace methods:**
- ❌ `IsMaoDe10Active()` → ✅ `IsLastHandActive()`
- ❌ `IsOneTeamAt10()` → ✅ `IsOneTeamAtLastHand()`  
- ❌ `AreBothTeamsAt10()` → ✅ `AreBothTeamsAtLastHand()`
- ❌ `GetTeamAt10()` → ✅ `GetTeamAtLastHand()`

**Update logic:**
- Replace hardcoded `10` with `TrucoConstants.IsLastHand(teamScore)`
- Replace hardcoded `12` with `TrucoConstants.MaximumStakes`
- Use `TrucoConstants.StakesProgression` for calculations

### 2.2 MappingService.cs (Available Actions)
**Current issues to fix:**
- ❌ Hardcoded stakes checking: `gameState.Stakes < TrucoConstants.Stakes.Maximum`
- ❌ "Mão de 10" references in comments and logic

**New logic:**
```csharp
// Replace current checks with:
bool isLastHandActive = TrucoConstants.IsLastHand(gameState.Team1Score) || 
                       TrucoConstants.IsLastHand(gameState.Team2Score);
bool isMaxStakes = TrucoConstants.IsMaximumStakes(gameState.Stakes);
bool canCallTruco = !isLastHandActive && !isMaxStakes;
```

### 2.3 GameStateMachine.cs
**Update command processing:**
- Use `TrucoConstants.GetNextStakes()` for truco progression
- Replace hardcoded TrucoCallState values
- Use `TrucoConstants.IsMaximumStakes()` for validation

## 🎴 Phase 3: New Features

### 3.1 Iron Hand Feature
**When:** Both teams at last hand + `IronHandFlag = true`
**Behavior:** Players can't see their own cards until played

**Implementation:**
```csharp
// In MappingService.MapPlayerToDto()
bool hideCards = TrucoConstants.IronHandFlag && 
                AreBothTeamsAtLastHand(gameState) && 
                !player.IsAI; // AIs still need card data

if (hideCards && !showAllHands) 
{
    // Set cards to null/hidden for human players
    playerDto.Hand = player.Hand.Select(c => CardDto.CreateHidden()).ToList();
}
```

### 3.2 Partner Card Visibility  
**When:** One team at last hand
**Behavior:** That team sees both player + partner cards

**Implementation:**
```csharp
// In MappingService.MapPlayerToDto()
bool showPartnerCards = IsOneTeamAtLastHand(gameState) && 
                       GetTeamAtLastHand(gameState) == player.Team;

if (showPartnerCards) 
{
    // Find partner and make their cards visible
    var partner = gameState.Players.FirstOrDefault(p => 
        p.Team == player.Team && p.Seat != player.Seat);
    
    if (partner != null) 
    {
        // Add partner's cards to visible hand or separate property
    }
}
```

### 3.3 Last Hand Behavior
**When:** One team at last hand  
**Behavior:** That team must choose surrender (2 points to opponent) or play (stakes = 4)

**Available Actions:**
```csharp
if (IsOneTeamAtLastHand(gameState)) 
{
    var teamAtLastHand = GetTeamAtLastHand(gameState);
    if (currentPlayer.Team == teamAtLastHand) 
    {
        // Team at last hand must decide
        actions.Add("surrender-last-hand"); // Award 2 points to opponent
        actions.Add("play-last-hand");      // Set stakes to 4, continue
    }
    // No truco calls allowed when any team at last hand
}
```

## 🧪 Phase 4: Test Updates

### 4.1 Update Test Constants
Replace all hardcoded values:
- ❌ `game.Team1Score = 10` → ✅ `game.Team1Score = TrucoConstants.VictoryPoints - TrucoConstants.MinimumStakes`
- ❌ `stakes == 12` → ✅ `stakes == TrucoConstants.MaximumStakes`
- ❌ `TrucoCallState.Doze` → ✅ `TrucoCallState.PendingResponse`

### 4.2 Rename Test Files
- ❌ `MaoDe10SpecialRulesTests.cs` → ✅ `LastHandSpecialRulesTests.cs`
- ❌ `MaoDe10AvailableActionsTests.cs` → ✅ `LastHandAvailableActionsTests.cs`

### 4.3 Update Test Methods
Replace test names and logic:
- ❌ `WhenOneTeamAt10_Should...` → ✅ `WhenOneTeamAtLastHand_Should...`
- ❌ `WhenBothTeamsAt10_Should...` → ✅ `WhenBothTeamsAtLastHand_Should...`

## 📋 Implementation Order

### Phase 1: Foundation (Days 1-2)
1. ✅ Update `TrucoConstants.cs` with new structure
2. ✅ Simplify `TrucoCallState` enum  
3. ✅ Update `TrucoRulesEngine.cs` method names and logic
4. ✅ Run tests to ensure no regressions

### Phase 2: Core Refactor (Days 2-3)  
5. ✅ Refactor `MappingService.cs` available actions logic
6. ✅ Update `GameStateMachine.cs` command processing
7. ✅ Update `AIPlayerService.cs` decision logic
8. ✅ Fix all compilation errors

### Phase 3: New Features (Days 3-4)
9. ✅ Implement Iron Hand feature
10. ✅ Add partner card visibility
11. ✅ Implement last hand behavior (surrender/play)
12. ✅ Add new available actions

### Phase 4: Testing (Days 4-5)
13. ✅ Update all test files with new constants
14. ✅ Rename test files and methods
15. ✅ Add tests for new features
16. ✅ Ensure all 117+ tests pass

### Phase 5: Validation (Day 5)
17. ✅ Full integration testing
18. ✅ Manual testing of new features
19. ✅ Documentation updates
20. ✅ Final commit and summary

## ⚠️ Risk Mitigation

### High Risk Areas
1. **Stakeholder progression logic** - Many files reference stakes values
2. **Available actions changes** - Core game flow logic
3. **Test updates** - Large number of tests to update

### Mitigation Strategies  
1. **Incremental commits** - Small, testable changes
2. **Frequent test runs** - Catch regressions early
3. **Rollback plan** - Can revert to current working state
4. **Feature flags** - Iron Hand can be disabled if issues

## 🎯 Success Criteria

### Technical
- ✅ All tests pass (117+ tests)
- ✅ No hardcoded stakes values in code
- ✅ Dynamic progression system working
- ✅ Iron Hand feature functional
- ✅ Partner card visibility working

### Functional  
- ✅ Last hand behavior correct (surrender/play choice)
- ✅ No truco calls when team at last hand
- ✅ Card visibility rules properly implemented
- ✅ Stakes progression follows Truco Mineiro rules

### Code Quality
- ✅ Clean, maintainable code structure
- ✅ Single source of truth for game constants
- ✅ Flexible foundation for future variants
- ✅ Comprehensive test coverage

---

**Ready to proceed with Phase 1: Foundation Refactor?**
