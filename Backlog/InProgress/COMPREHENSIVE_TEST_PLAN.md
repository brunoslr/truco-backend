# 🧪 COMPREHENSIVE TEST PLAN - Event-Driven Truco Backend

**Date:** June 10, 2025  
**Status:** 📋 PLANNING PHASE - BUSINESS REQUIREMENTS VALIDATION  
**Objective:** Zero failing tests + Complete business requirement coverage

## 🎯 ACCEPTANCE CRITERIA

### ✅ Technical Requirements
- [ ] **Zero compilation errors** in all test projects
- [ ] **Zero failing tests** across all test suites
- [ ] **Zero warnings** in test builds
- [ ] **100% event-driven architecture** alignment in tests
- [ ] **Type-safe Guid usage** throughout all tests

### ✅ Business Requirements Coverage
- [ ] **Complete game flow** from start to finish
- [ ] **All Truco rules** properly validated
- [ ] **AI behavior** thoroughly tested
- [ ] **Edge cases** and error scenarios covered
- [ ] **Performance requirements** validated

---

## 📊 CURRENT TEST FAILURE ANALYSIS

### 🔴 Compilation Errors (13 total)
1. **GameStateMachineTests.cs** - String to Guid conversion issues
2. **EventPublisherTests.cs** - Player.Id type mismatches  
3. **EventDrivenAIPlayerTests.cs** - Guid parsing errors

### 🔴 Architecture Misalignment Issues
- Tests using obsolete synchronous methods
- Mock-heavy tests instead of real event handlers
- Outdated test patterns not matching event-driven flow

---

## 🏗️ BUSINESS REQUIREMENTS VALIDATION

I need your validation on the business requirements for each domain entity before proceeding with the comprehensive test plan.

### 🎮 **GAME ENTITY - Business Requirements** ✅ VALIDATED

#### Game Structure Hierarchy
```
GAME (Complete Match)
├── HAND 1 (Best of 3 rounds)
│   ├── ROUND 1 (Each player plays 1 card)
│   ├── ROUND 2 (Each player plays 1 card) 
│   └── ROUND 3 (Each player plays 1 card) - if needed
├── HAND 2 (Best of 3 rounds)
└── ... until one team reaches 12 points
```

#### Detailed Entity Breakdown

1. **ROUND (Turno/Volta)**
   - ✅ **Definition:** Each player plays exactly one card in turn order
   - ✅ **Turn Order:** Starting player → clockwise rotation → all 4 players
   - ✅ **Winner:** Player with highest card value (considering Truco hierarchy)
   - ✅ **Result:** Round winner starts next round
   - ✅ **Duration:** Exactly 4 card plays (one per player)

2. **HAND (Mão)**
   - ✅ **Definition:** Best of 3 rounds competition
   - ✅ **Card Distribution:** 3 cards dealt to each player at start
   - ✅ **Winning Condition:** First team to win 2 rounds wins the hand
   - ✅ **Early Termination:** Hand can end after 2 rounds if one team wins both
   - ✅ **Stakes:** Current stake points awarded to hand winner
   - ✅ **Dealer Rotation:** Dealer moves clockwise after each hand

3. **GAME (Jogo/Partida)**
   - ✅ **Definition:** Complete match consisting of multiple hands
   - ✅ **Winning Condition:** First team to reach exactly 12 points
   - ✅ **Scoring:** Cumulative points from won hands
   - ✅ **Early Termination:** Game ends immediately when team reaches 12 points
   - ✅ **Duration:** Variable (depends on Truco calls and hand outcomes)

#### Game Lifecycle
4. **Game Creation**
   - ✅ Game must support configurable player setup (default: 1 human + 3 AI)
   - ✅ Game must assign unique Guid IDs to all players
   - ✅ Game must start with dealer randomly selected
   - ✅ Game must initialize with 2-point stakes
   - ✅ Teams must be fixed: (Seat 0 + Seat 2) vs (Seat 1 + Seat 3)

#### Game State Management
5. **Game Progression**
   - ✅ Game progresses through multiple hands until one team reaches 12 points
   - ✅ Each hand consists of up to 3 rounds (best of 3)
   - ✅ Dealer rotates clockwise after each hand
   - ✅ Round winner plays first in next round
   - ✅ Hand winner's team gets first player position in next hand

#### Game Completion
6. **Winning Conditions**
   - ✅ Game ends when any team reaches exactly 12 points
   - ✅ Game can end early if a team folds (opponent gets current stakes)
   - ✅ Final scores are tracked per team, not individual players
   - ✅ Game duration and final scores must be recorded

---

### 🎭 **PLAYER ENTITY - Business Requirements** ✅ VALIDATED

#### Player Configuration System
7. **Configurable Player Setup**
   - ✅ **Default Setup:** 1 Human (seat 0) + 3 AI (seats 1,2,3)
   - ✅ **Future Flexibility:** Support any mix of Human/AI players
   - ✅ **Team Awareness:** AI must know their partner's position
   - ✅ **Fixed Team Structure:** 
     - Team 1: Seat 0 + Seat 2 (always partners)
     - Team 2: Seat 1 + Seat 3 (always partners)

#### Player Types & Behavior
8. **Human Player**
   - ✅ Can be assigned to any seat (default: seat 0)
   - ✅ Requires manual input for all actions
   - ✅ Cannot auto-play cards
   - ✅ Can call Truco, raise stakes, or fold
   - ✅ Receives only their own cards (unless DevMode enabled)

9. **AI Players**
   - ✅ Can be assigned to any seat (default: seats 1,2,3)
   - ✅ Must auto-play cards when their turn starts
   - ✅ Must have configurable thinking delay (1-3 seconds)
   - ✅ Can make strategic decisions (Truco, fold, etc.)
   - ✅ Must be aware of partner position for strategic play
   - ✅ Cards are never sent to frontend (unless DevMode enabled)

#### Player State Management
10. **Player Status**
    - ✅ Only one player can be active at a time
    - ✅ Players must be assigned to fixed teams based on seat position
    - ✅ Player hands must contain exactly 3 cards at start of each hand
    - ✅ Dealer status rotates each hand
    - ✅ Player turn order follows clockwise rotation

---

### 🃏 **CARD & HAND ENTITY - Business Requirements**

#### Card Management
7. **Card Distribution**
   - ✅ Each player gets exactly 3 cards per hand
   - ✅ Cards are dealt from a properly shuffled 40-card deck
   - ✅ No duplicate cards can exist in play
   - ✅ Cards have proper Truco values and suit hierarchy
   
   **❓ VALIDATION NEEDED:** Are these card distribution rules correct?

#### Hand Resolution
8. **Round Winning Logic**
   - ✅ Highest card value wins each round
   - ✅ Suit hierarchy applies for equal values
   - ✅ Round winner plays first in next round
   - ✅ Hand ends when team wins 2 out of 3 rounds
   
   **❓ VALIDATION NEEDED:** Are these hand resolution rules accurate?

---

### 🏆 **TRUCO MECHANICS - Business Requirements**

#### Stake Management
9. **Truco Calling**
   - ✅ Initial stakes: 2 points
   - ✅ First Truco call: raises to 4 points
   - ✅ Subsequent raises: +4 points each (4→8→12)
   - ✅ Maximum stakes: 12 points
   
   **❓ VALIDATION NEEDED:** Are these stake progression rules correct?

10. **Truco Responses**
    - ✅ Opponent can accept (continue with new stakes)
    - ✅ Opponent can raise (increase stakes further)
    - ✅ Opponent can fold (caller wins current stakes)
    - ✅ Only opposing team members can respond
    
    **❓ VALIDATION NEEDED:** Are these response options correct?

---

### ⚡ **EVENT-DRIVEN ARCHITECTURE - Technical Requirements**

#### Event Flow Validation
11. **Core Event Patterns**
    - ✅ All game actions must trigger appropriate events
    - ✅ AI responses must be event-driven, not synchronous
    - ✅ Game state changes must be handled through events
    - ✅ No direct service-to-service calls for game logic
    
    **❓ VALIDATION NEEDED:** Are these event-driven patterns correct?

#### Event Types Coverage
12. **Required Events**
    - ✅ GameStartedEvent, PlayerTurnStartedEvent, CardPlayedEvent
    - ✅ RoundCompletedEvent, HandCompletedEvent, GameCompletedEvent
    - ✅ TrucoRaiseEvent, SurrenderHandEvent, ActionLogEvent
    - ✅ All events must carry proper Guid identifiers
    
    **❓ VALIDATION NEEDED:** Are these the complete set of required events?

---

## ❓ VALIDATION REQUEST

**Please review and confirm each business requirement section above:**

1. **Game Entity requirements** - Are they accurate?
2. **Player Entity requirements** - Any missing or incorrect rules?
3. **Card & Hand requirements** - Do they match actual Truco rules?
4. **Truco Mechanics** - Are the stake progressions correct?
5. **Event Architecture** - Any additional event patterns needed?

**Once you validate these requirements, I'll create the detailed implementation plan with:**
- Specific test fixes for each compilation error
- Comprehensive integration test scenarios
- Business rule validation test cases
- Performance and edge case coverage
- Event-driven architecture compliance tests

**Please confirm which requirements are correct or suggest modifications for any that need adjustment.**
