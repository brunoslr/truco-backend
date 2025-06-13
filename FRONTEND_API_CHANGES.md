# Frontend API Changes - Truco Stakes Progression

## Breaking Changes in Button Press Actions

### ⚠️ **BREAKING CHANGE**: Button Press Actions Updated

The button press action values have been modernized to support the new event-driven truco system.

#### Old Actions (DEPRECATED - Removed in this version)
```javascript
// ❌ NO LONGER SUPPORTED
"truco"     // Old truco call
"raise"     // Old raise action  
"surrender" // Old surrender action
```

#### New Actions (Required)
```javascript
// ✅ NEW REQUIRED ACTIONS
"CallTrucoOrRaise"  // Unified truco call/raise action
"AcceptTruco"       // Accept a truco call
"SurrenderTruco"    // Surrender to a truco call
```

### API Endpoint Changes

#### Button Press Endpoint: `POST /api/game/press-button`

**Request Body** (unchanged structure, updated action values):
```json
{
  "gameId": "string",
  "playerSeat": 0,
  "action": "CallTrucoOrRaise"  // Use new action values
}
```

**Success Response** (returns full game state):
```json
{
  "players": [...],
  "trucoCallState": "Truco",
  "currentStakes": 4,
  "lastTrucoCallerTeam": 1,
  "canRaiseTeam": 2,
  "availableActions": ["accept-truco", "surrender-truco"],  // NEW: Dynamic actions
  // ... other game state properties
}
```

**Error Response** for invalid actions:
```json
HTTP 400 Bad Request
"Invalid action: old_action. Valid actions are: CallTrucoOrRaise, AcceptTruco, SurrenderTruco, SurrenderHand"
```

### Game State Changes

#### New Properties in GameStateDto
```json
{
  "trucoCallState": "None|Truco|Seis|Doze",  // Replaces trucoLevel
  "lastTrucoCallerTeam": 0,                  // Team that made last call
  "canRaiseTeam": 1,                         // Team that can raise (null if none)
  "isBothTeamsAt10": false,                  // "Mão de 10" detection
  "currentStakes": 4,                        // Current points at stake
  "availableActions": [                      // NEW: Dynamic action list for current player
    "play-card",                             // Can play a card
    "call-truco-or-raise",                   // Can call truco or raise
    "accept-truco",                          // Can accept truco call
    "surrender-truco",                       // Can surrender to truco
    "fold"                                   // Can fold hand
  ]
}
```

#### Removed Properties
```json
// ❌ REMOVED - No longer available
{
  "isTrucoCalled": false,        // Use trucoCallState instead
  "isRaiseEnabled": false,       // Use canRaiseTeam instead  
  "trucoLevel": 1,               // Use trucoCallState instead
  "trucoCalledBy": "guid",       // Use lastTrucoCallerTeam instead
  "waitingForTrucoResponse": false // Use trucoCallState logic instead
}
```

### Frontend Migration Guide

#### 1. Update Button Action Logic
```javascript
// Before (old approach)
function callTruco() {
  sendButtonPress({ action: "truco" });
}

function raiseStakes() {
  sendButtonPress({ action: "raise" });
}

// After (new unified approach)
function callTrucoOrRaise() {
  sendButtonPress({ action: "CallTrucoOrRaise" });
}

function acceptTruco() {
  sendButtonPress({ action: "AcceptTruco" });
}

function surrenderTruco() {
  sendButtonPress({ action: "SurrenderTruco" });
}
```

#### 2. Update Game State Reading
```javascript
// Before (checking old properties)
const canCallTruco = !gameState.isTrucoCalled && gameState.isRaiseEnabled;
const waitingForResponse = gameState.waitingForTrucoResponse;

// After (using new state model)
const canCallTruco = gameState.trucoCallState !== "Doze" && 
                     !gameState.isBothTeamsAt10 &&
                     gameState.lastTrucoCallerTeam !== currentPlayerTeam;

const hasPendingCall = gameState.trucoCallState !== "None" && 
                       gameState.lastTrucoCallerTeam !== currentPlayerTeam;
```

#### 3. Update UI Button Visibility
```javascript
// Before
const showTrucoButton = !gameState.isTrucoCalled;
const showRaiseButton = gameState.isTrucoCalled && gameState.isRaiseEnabled;
const showResponseButtons = gameState.waitingForTrucoResponse;

// After  
const playerTeam = getCurrentPlayerTeam();
const hasPendingCall = gameState.trucoCallState !== "None" && 
                       gameState.lastTrucoCallerTeam !== playerTeam;

const showTrucoRaiseButton = !gameState.isBothTeamsAt10 && 
                             gameState.trucoCallState !== "Doze" &&
                             gameState.lastTrucoCallerTeam !== playerTeam;

const showResponseButtons = hasPendingCall;
```

### Stakes Display Updates

#### New Stakes Progression Logic
```javascript
// Truco stakes progression
const stakesMap = {
  "None": 2,    // Base hand value
  "Truco": 4,   // After Truco call
  "Seis": 8,    // After Seis raise
  "Doze": 12    // Maximum stakes
};

const currentStakes = gameState.currentStakes;
const potentialStakes = stakesMap[gameState.trucoCallState] || 2;
```

### Special Rules Implementation

#### "Mão de 10" Detection
```javascript
// Check if truco is disabled due to "Mão de 10"
if (gameState.isBothTeamsAt10) {
  // Hide truco/raise buttons
  // Show "Mão de 10" indicator
  // Stakes automatically set to 4 points
}
```

### 🚀 **NEW FEATURE**: Dynamic Available Actions

The `availableActions` array in the game state response tells the frontend exactly which buttons to show for the current player. This eliminates complex frontend logic for determining button visibility.

#### Available Action Values
```javascript
const AVAILABLE_ACTIONS = {
  PLAY_CARD: "play-card",              // Player can play a card
  CALL_TRUCO_OR_RAISE: "call-truco-or-raise", // Player can call truco or raise
  ACCEPT_TRUCO: "accept-truco",        // Player can accept truco call
  SURRENDER_TRUCO: "surrender-truco",  // Player can surrender to truco
  FOLD: "fold"                         // Player can fold the hand
};
```

#### Implementation Example
```javascript
// Simple button visibility logic
function updateButtonVisibility(gameState) {
  const actions = gameState.availableActions;
  
  // Show/hide buttons based on available actions
  playCardButton.style.display = actions.includes("play-card") ? "block" : "none";
  trucoButton.style.display = actions.includes("call-truco-or-raise") ? "block" : "none";
  acceptButton.style.display = actions.includes("accept-truco") ? "block" : "none";
  surrenderButton.style.display = actions.includes("surrender-truco") ? "block" : "none";
  foldButton.style.display = actions.includes("fold") ? "block" : "none";
}
```

#### Truco Flow Behavior
- **After calling truco**: Calling player has NO available actions (waits for response)
- **Responding to truco**: Responding team gets `["accept-truco", "surrender-truco", "call-truco-or-raise"]`
- **Normal game**: Players get `["play-card", "call-truco-or-raise", "fold"]` (if truco rules allow)
- **"Mão de 10"**: Truco actions are excluded, only `["play-card", "fold"]` available
```

### Testing Checklist

#### Button Actions & API
- [ ] Update all button click handlers to use new action values (`CallTrucoOrRaise`, `AcceptTruco`, `SurrenderTruco`)
- [ ] Test button press endpoint with correct URL: `/api/game/press-button`
- [ ] Verify error messages are properly displayed for invalid actions

#### Game State Integration  
- [ ] Remove references to deprecated game state properties (`isTrucoCalled`, `isRaiseEnabled`, etc.)
- [ ] Implement `availableActions` array for dynamic button visibility
- [ ] Test that buttons appear/disappear correctly based on game state

#### Truco Flow Testing
- [ ] Test truco call flow: call → opponent gets response options → caller waits
- [ ] Test accept/surrender response flow with point awarding
- [ ] Test raise progression: Truco → Seis → Doze → maximum reached
- [ ] Verify consecutive call prevention (same team cannot call twice)

#### Special Rules
- [ ] Verify "Mão de 10" rule enforcement (truco disabled when both teams at 10 points)
- [ ] Test stakes progression display (2→4→8→12)
- [ ] Validate team alternation logic for truco calls

#### Error Handling
- [ ] Test network error handling for button press requests
- [ ] Verify proper error messages for invalid game states
- [ ] Test validation error display for malformed requests

### Backward Compatibility

**No backward compatibility** is provided for the old button actions. All frontend code must be updated to use the new action values.

### Support

For questions about this migration, refer to:
- Game state model: `TrucoMineiro.API/Domain/Models/GameState.cs`
- Button actions: `TrucoMineiro.API/DTOs/ButtonPressRequest.cs`  
- Business rules: `TrucoMineiro.API/Domain/Services/TrucoRulesEngine.cs`
