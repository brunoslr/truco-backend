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

#### Button Press Endpoint: `POST /api/truco/button-press`

**Request Body** (unchanged structure, updated action values):
```json
{
  "gameId": "string",
  "playerSeat": 0,
  "action": "CallTrucoOrRaise"  // Use new action values
}
```

**Error Response** for invalid actions:
```json
{
  "error": "Invalid action: old_action. Valid actions are: CallTrucoOrRaise, AcceptTruco, SurrenderTruco"
}
```

### Game State Changes

#### New Properties in GameStateDto
```json
{
  "trucoCallState": "None|Truco|Seis|Doze",  // Replaces trucoLevel
  "lastTrucoCallerTeam": 0,                  // Team that made last call
  "canRaiseTeam": 1,                         // Team that can raise (null if none)
  "isBothTeamsAt10": false,                  // "Mão de 10" detection
  "currentStakes": 4                         // Current points at stake
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

### Testing Checklist

- [ ] Update all button click handlers to use new action values
- [ ] Remove references to deprecated game state properties
- [ ] Test truco call/raise flow with new unified button
- [ ] Test accept/surrender response flow
- [ ] Verify "Mão de 10" rule enforcement
- [ ] Test stakes progression display (2→4→8→12)
- [ ] Validate team alternation logic
- [ ] Test error handling for invalid actions

### Backward Compatibility

**No backward compatibility** is provided for the old button actions. All frontend code must be updated to use the new action values.

### Support

For questions about this migration, refer to:
- Game state model: `TrucoMineiro.API/Domain/Models/GameState.cs`
- Button actions: `TrucoMineiro.API/DTOs/ButtonPressRequest.cs`  
- Business rules: `TrucoMineiro.API/Domain/Services/TrucoRulesEngine.cs`
