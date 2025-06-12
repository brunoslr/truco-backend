# 🎮 Frontend Integration Guide - Truco Mineiro Backend

## 🚀 Quick Start

The Truco Mineiro backend is now **production-ready** and fully tested! Here's everything you need to integrate with the frontend.

### 📡 Server Information

- **Base URL**: `http://localhost:5084` (Development)
- **HTTPS URL**: `https://localhost:7120` (Development with SSL)
- **Health Check**: `GET /api/game/health`

---

## 🎯 Core API Endpoints

### 1. 🏁 Start New Game

```http
POST /api/game/start
Content-Type: application/json

{
  "playerName": "Your Name"
}
```

**Response:**
```json
{
  "gameId": "e17bdb8e-bfc4-4d36-aab3-ed8bdd0a58cf",
  "playerSeat": 0,
  "teams": [
    {"name": "Player's Team", "seats": [0, 2]},
    {"name": "Opponent Team", "seats": [1, 3]}
  ],
  "players": [
    {"seat": 0, "name": "Your Name", "team": "Player's Team"},
    {"seat": 1, "name": "AI 1", "team": "Opponent Team"},
    {"seat": 2, "name": "Partner", "team": "Player's Team"},
    {"seat": 3, "name": "AI 2", "team": "Opponent Team"}
  ],
  "hand": [
    {"value": "5", "suit": "♦"},
    {"value": "J", "suit": "♦"},
    {"value": "2", "suit": "♦"}
  ],
  "dealerSeat": 3,
  "teamScores": {"Player's Team": 0, "Opponent Team": 0},
  "stakes": 2,
  "currentHand": 1
}
```

### 2. 🃏 Play Card

```http
POST /api/game/play-card
Content-Type: application/json

{
  "gameId": "e17bdb8e-bfc4-4d36-aab3-ed8bdd0a58cf",
  "playerSeat": 0,
  "cardIndex": 0,
  "isFold": false
}
```

**Response:**
```json
{
  "success": true,
  "message": "Card played successfully",
  "gameState": {
    "players": [...],
    "playedCards": [
      {"playerSeat": 0, "card": {"value": "5", "suit": "♦"}},
      {"playerSeat": 1, "card": {"value": "Q", "suit": "♥"}},
      {"playerSeat": 2, "card": {"value": "A", "suit": "♦"}},
      {"playerSeat": 3, "card": {"value": "4", "suit": "♠"}}
    ],
    "stakes": 2,
    "currentHand": 1,
    "teamScores": {...},
    "actionLog": [...]
  },
  "hand": [...],
  "playerHands": [...]
}
```

### 3. 📊 Get Game State

```http
GET /api/game/{gameId}?playerSeat={playerSeat}
```

### 4. 🎲 Call Truco

```http
POST /api/game/call-truco
Content-Type: application/json

{
  "gameId": "game-id-here",
  "playerSeat": 0
}
```

### 5. 🏳️ Fold Hand

```http
POST /api/game/fold
Content-Type: application/json

{
  "gameId": "game-id-here",
  "playerSeat": 0
}
```

---

## 🤖 AI Auto-Play Features

### ✨ Event-Driven AI System

The backend uses a **sophisticated event-driven architecture**:

- **Automatic AI Response**: When a human player plays a card, AI players automatically play their cards
- **Realistic Timing**: AI players have configurable thinking delays for natural gameplay
- **Smart Decision Making**: AI players use strategic logic for card selection and Truco calls

### ⚙️ Configuration Options

You can configure AI behavior via environment variables or `appsettings.json`:

```json
{
  "FeatureFlags": {
    "AutoAiPlay": true,     // Enable/disable AI auto-play
    "DevMode": false        // Show/hide AI cards for debugging
  },
  "GameSettings": {
    "AIPlayDelayMs": 2000,  // AI thinking delay (milliseconds)
    "NewHandDelayMs": 1000  // Delay between hands
  }
}
```

---

## 🎨 Card Visibility System

### 🔒 Normal Mode (Production)
- **Human Player**: Can see their own cards
- **AI Players**: Cards are hidden (null values)
- **Played Cards**: Always visible to everyone

### 🔍 DevMode (Development)
- **All Players**: Can see all cards (useful for testing)
- **Enable**: Set `FeatureFlags:DevMode = true`

---

## 🎯 Game Flow

### 📋 Typical Game Sequence

1. **Frontend calls** `/api/game/start`
2. **Backend creates** game with 1 human + 3 AI players
3. **Frontend displays** player's hand and game state
4. **Human player** calls `/api/game/play-card`
5. **Backend processes** card play and publishes event
6. **AI players automatically** respond via event system
7. **Frontend receives** updated game state with all moves
8. **Repeat** until hand/game completion

### 🔄 Event-Driven Benefits

- **Real-time gameplay**: AI responses happen automatically
- **No polling needed**: Single API call triggers complete round
- **Scalable**: Event system can handle multiple concurrent games
- **Testable**: Comprehensive test coverage ensures reliability

---

## 📱 Frontend Implementation Tips

### 🎮 Game State Management

```javascript
class TrucoGame {
  constructor() {
    this.gameId = null;
    this.playerSeat = 0;
    this.gameState = null;
  }

  async startGame(playerName) {
    const response = await fetch('/api/game/start', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ playerName })
    });
    
    const data = await response.json();
    this.gameId = data.gameId;
    this.playerSeat = data.playerSeat;
    return data;
  }

  async playCard(cardIndex, isFold = false) {
    const response = await fetch('/api/game/play-card', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        gameId: this.gameId,
        playerSeat: this.playerSeat,
        cardIndex,
        isFold
      })
    });
    
    const data = await response.json();
    this.gameState = data.gameState;
    return data;
  }
}
```

### 🎨 Card Display

```javascript
function renderCard(card) {
  if (!card.value || !card.suit) {
    return '<div class="card hidden">🂠</div>'; // Hidden card
  }
  
  return `<div class="card">
    <span class="value">${card.value}</span>
    <span class="suit">${card.suit}</span>
  </div>`;
}
```

### 🔔 Action Log Processing

```javascript
function processActionLog(actions) {
  return actions.map(action => {
    switch(action.type) {
      case 'card-played':
        return `${getPlayerName(action.playerSeat)} played ${action.card}`;
      case 'turn-result':
        return `${action.winner} won the round`;
      case 'hand-result':
        return `Hand ${action.handNumber} won by ${action.winnerTeam}`;
      default:
        return action.type;
    }
  });
}
```

---

## 🧪 Testing & Development

### 🔍 Health Check

Always verify backend availability:

```javascript
async function checkBackendHealth() {
  try {
    const response = await fetch('/api/game/health');
    const health = await response.json();
    console.log('Backend status:', health.status);
    return health.status === 'healthy';
  } catch (error) {
    console.error('Backend not available:', error);
    return false;
  }
}
```

### 🎯 DevMode Testing

Enable DevMode to see all cards during development:

```json
{
  "FeatureFlags": {
    "DevMode": true
  }
}
```

### 🤖 AI Behavior Testing

Disable AI auto-play to test manual gameplay:

```json
{
  "FeatureFlags": {
    "AutoAiPlay": false
  }
}
```

---

## 🚀 Production Deployment

### 🌐 Environment Variables

```bash
# Production settings
ASPNETCORE_ENVIRONMENT=Production
FeatureFlags__DevMode=false
FeatureFlags__AutoAiPlay=true
GameSettings__AIPlayDelayMs=3000
```

### 🔒 CORS Configuration

The backend includes CORS support for frontend integration. Update `Program.cs` for production origins:

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("https://yourdomain.com")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
```

---

## 📊 Performance Notes

- **Response Time**: ~50-200ms for card plays
- **AI Response**: 1-3 seconds (configurable delay)
- **Memory Usage**: Optimized for multiple concurrent games
- **Database**: In-memory (configurable for Redis/SQL)

---

## 🆘 Troubleshooting

### Common Issues

1. **Game not found**: Ensure gameId is correctly stored after `/start`
2. **Invalid card index**: Check hand array bounds before playing
3. **Player turn validation**: Only active player can play cards
4. **AI not responding**: Verify `AutoAiPlay` is enabled

### Debug Logging

Enable detailed logging in `appsettings.Development.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "TrucoMineiro.API.Domain.EventHandlers": "Debug"
    }
  }
}
```

---

## 🎉 Ready to Play!

The backend is **production-ready** with:

- ✅ **74 tests passing** (100% success rate)
- ✅ **Event-driven AI system**
- ✅ **Complete Truco gameplay**
- ✅ **Comprehensive error handling**
- ✅ **DevMode for development**
- ✅ **Clean, maintainable code**

**Happy coding! 🎮🃏**
