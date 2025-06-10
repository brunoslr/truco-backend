# Event-Driven Architecture Migration Plan

## Overview

This document outlines the complete migration plan from the current synchronous, tightly-coupled architecture to an event-driven, multiplayer-ready system. The migration is designed to be incremental, maintaining functionality at each step.

## Current State Analysis

### Problems to Solve
- ❌ Synchronous AI processing blocks multiplayer scenarios
- ❌ Tightly coupled services make testing and scaling difficult
- ❌ No real-time notifications for multiplayer
- ❌ AI players are not treated as first-class citizens
- ❌ No event sourcing or replay capabilities
- ❌ Difficult to add features like spectators, timeouts, reconnections

### Target Architecture
```
┌─────────────────┐    Commands    ┌──────────────────┐
│   Controllers   │──────────────► │  Game State      │
│                 │                │  Machine         │
└─────────────────┘                └──────────────────┘
                                            │
                                            │ Events
                                            ▼
┌─────────────────┐              ┌──────────────────┐
│  Event Handlers │◄─────────────┤  Event Publisher │
│  - AI Players   │              │                  │
│  - Notifications│              └──────────────────┘
│  - Persistence  │
│  - Analytics    │
└─────────────────┘
```

## Migration Phases

### Phase 1: Foundation - Event System Infrastructure
**Duration**: 1-2 days  
**Risk Level**: Low  
**Goal**: Add event infrastructure alongside current system

### Phase 2: Event-Driven AI
**Duration**: 2-3 days  
**Risk Level**: Medium  
**Goal**: Make AI players reactive to events instead of synchronous

### Phase 3: Game State Machine
**Duration**: 3-4 days  
**Risk Level**: High  
**Goal**: Replace synchronous game flow with state machine

### Phase 4: Real-time Multiplayer
**Duration**: 2-3 days  
**Risk Level**: Medium  
**Goal**: Add SignalR and real-time notifications

### Phase 5: Advanced Features
**Duration**: 2-3 days  
**Risk Level**: Low  
**Goal**: Add timeouts, reconnections, spectators

---

## Phase 1: Foundation - Event System Infrastructure

### Step 1.1: Create Event System Base Classes

**Create**: `TrucoMineiro.API/Domain/Events/IGameEvent.cs`
```csharp
using System;

namespace TrucoMineiro.API.Domain.Events
{
    public interface IGameEvent
    {
        string GameId { get; }
        DateTime Timestamp { get; }
        string EventType { get; }
        string EventId { get; }
    }

    public abstract class GameEventBase : IGameEvent
    {
        public string GameId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public abstract string EventType { get; }
        public string EventId { get; set; } = Guid.NewGuid().ToString();
    }
}
```

**Create**: `TrucoMineiro.API/Domain/Events/GameEvents.cs`
```csharp
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Events
{
    public class CardPlayedEvent : GameEventBase
    {
        public override string EventType => "CardPlayed";
        public int PlayerSeat { get; set; }
        public Card Card { get; set; } = new();
        public bool IsFold { get; set; }
    }

    public class PlayerTurnStartedEvent : GameEventBase
    {
        public override string EventType => "PlayerTurnStarted";
        public int PlayerSeat { get; set; }
        public TimeSpan? TimeLimit { get; set; }
    }

    public class RoundCompletedEvent : GameEventBase
    {
        public override string EventType => "RoundCompleted";
        public int WinningSeat { get; set; }
        public List<PlayedCard> PlayedCards { get; set; } = new();
    }

    public class HandCompletedEvent : GameEventBase
    {
        public override string EventType => "HandCompleted";
        public int WinningTeam { get; set; }
        public int PointsAwarded { get; set; }
    }

    public class TrucoCalledEvent : GameEventBase
    {
        public override string EventType => "TrucoCalled";
        public int CallerSeat { get; set; }
        public int NewStake { get; set; }
        public List<int> RespondingPlayers { get; set; } = new();
    }

    public class TrucoResponseEvent : GameEventBase
    {
        public override string EventType => "TrucoResponse";
        public int ResponderSeat { get; set; }
        public TrucoResponse Response { get; set; }
        public int? NewStake { get; set; }
    }

    public class GameStateChangedEvent : GameEventBase
    {
        public override string EventType => "GameStateChanged";
        public object? PreviousState { get; set; }
        public object? NewState { get; set; }
        public string ChangeReason { get; set; } = string.Empty;
    }
}
```

### Step 1.2: Create Event Publisher System

**Create**: `TrucoMineiro.API/Domain/Events/IEventPublisher.cs`
```csharp
namespace TrucoMineiro.API.Domain.Events
{
    public interface IEventPublisher
    {
        Task PublishAsync<T>(T gameEvent) where T : IGameEvent;
        Task PublishAsync<T>(T gameEvent, CancellationToken cancellationToken = default) where T : IGameEvent;
    }

    public interface IEventHandler<T> where T : IGameEvent
    {
        Task HandleAsync(T gameEvent, CancellationToken cancellationToken = default);
    }
}
```

**Create**: `TrucoMineiro.API/Domain/Services/InMemoryEventPublisher.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;

namespace TrucoMineiro.API.Domain.Services
{
    public class InMemoryEventPublisher : IEventPublisher
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<InMemoryEventPublisher> _logger;

        public InMemoryEventPublisher(IServiceProvider serviceProvider, ILogger<InMemoryEventPublisher> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        public async Task PublishAsync<T>(T gameEvent) where T : IGameEvent
        {
            await PublishAsync(gameEvent, CancellationToken.None);
        }

        public async Task PublishAsync<T>(T gameEvent, CancellationToken cancellationToken = default) where T : IGameEvent
        {
            try
            {
                _logger.LogDebug("Publishing event {EventType} for game {GameId}", gameEvent.EventType, gameEvent.GameId);

                var handlers = _serviceProvider.GetServices<IEventHandler<T>>();
                var tasks = handlers.Select(handler => 
                    HandleSafely(handler, gameEvent, cancellationToken));

                await Task.WhenAll(tasks);

                _logger.LogDebug("Successfully published event {EventType} to {HandlerCount} handlers", 
                    gameEvent.EventType, handlers.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing event {EventType} for game {GameId}", 
                    gameEvent.EventType, gameEvent.GameId);
                throw;
            }
        }

        private async Task HandleSafely<T>(IEventHandler<T> handler, T gameEvent, CancellationToken cancellationToken) 
            where T : IGameEvent
        {
            try
            {
                await handler.HandleAsync(gameEvent, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in event handler {HandlerType} for event {EventType}", 
                    handler.GetType().Name, gameEvent.EventType);
                // Don't rethrow - we want other handlers to continue
            }
        }
    }
}
```

### Step 1.3: Create ActionLog Event Handler

**Create**: `TrucoMineiro.API/Domain/EventHandlers/ActionLogEventHandler.cs`
```csharp
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for creating ActionLogEntry records from game events for frontend display
    /// This handler ensures that game events are properly logged as ActionLogEntry records
    /// that are displayed in the frontend's action log for players to see game history.
    /// </summary>
    public class ActionLogEventHandler : 
        IEventHandler<CardPlayedEvent>,
        IEventHandler<PlayerTurnStartedEvent>,
        IEventHandler<RoundCompletedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly ILogger<ActionLogEventHandler> _logger;

        public ActionLogEventHandler(
            IGameRepository gameRepository,
            ILogger<ActionLogEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _logger = logger;
        }

        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
            if (game == null) return;

            // Create action log entry for card played - this shows in frontend
            var cardDisplay = gameEvent.IsFold ? "FOLD" : $"{gameEvent.Card.Value} of {gameEvent.Card.Suit}";
            var actionLogEntry = new ActionLogEntry("card-played")
            {
                PlayerSeat = gameEvent.Player.Seat,
                Card = cardDisplay
            };

            game.ActionLog.Add(actionLogEntry);
            await _gameRepository.SaveGameAsync(game);
        }

        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            // Only log turn starts for human players to avoid cluttering action log
            if (!gameEvent.Player.IsAI)
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null) return;

                var actionLogEntry = new ActionLogEntry("turn-start")
                {
                    PlayerSeat = gameEvent.Player.Seat
                };

                game.ActionLog.Add(actionLogEntry);
                await _gameRepository.SaveGameAsync(game);
            }
        }

        public async Task HandleAsync(RoundCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
            if (game == null) return;

            // Create action log entry for round result - shows who won the round
            var winner = gameEvent.RoundWinner?.Name ?? "Draw";
            var actionLogEntry = new ActionLogEntry("turn-result")
            {
                Winner = winner,
                WinnerTeam = gameEvent.RoundWinner != null ? (gameEvent.RoundWinner.Seat % 2 == 0 ? "Team 1" : "Team 2") : null
            };

            game.ActionLog.Add(actionLogEntry);
            await _gameRepository.SaveGameAsync(game);
        }
    }
}
```

### Step 1.4: Update Dependency Injection

**Modify**: `TrucoMineiro.API/Program.cs`
```csharp
// Add after existing service registrations
builder.Services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
builder.Services.AddScoped<IEventHandler<CardPlayedEvent>, ActionLogEventHandler>();
builder.Services.AddScoped<IEventHandler<PlayerTurnStartedEvent>, ActionLogEventHandler>();
builder.Services.AddScoped<IEventHandler<RoundCompletedEvent>, ActionLogEventHandler>();
```

### Step 1.5: Add Events to Existing GameService (Parallel)

**Modify**: `TrucoMineiro.API/Domain/Services/GameService.cs`
```csharp
// Add constructor parameter
private readonly IEventPublisher _eventPublisher;

// Add to constructor
public GameService(
    IGameRepository gameRepository,
    IGameFlowService gameFlowService,
    ITrucoRulesEngine trucoRulesEngine,
    IScoreCalculationService scoreCalculationService,
    IGameFlowReactionService gameFlowReactionService,
    IEventPublisher eventPublisher,  // <-- Add this
    IConfiguration configuration)
{
    // ... existing assignments
    _eventPublisher = eventPublisher;
}

// Modify PlayCard method to publish events
public PlayCardResponseDto PlayCard(string gameId, int playerSeat, int cardIndex, bool isFold = false, int requestingPlayerSeat = 0)
{
    // ... existing logic until card is played

    // Publish event AFTER card is played successfully
    if (isFold)
    {
        var foldSuccess = HandleFoldAction(game, playerSeat);
        if (!foldSuccess)
        {
            return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Cannot fold at this time");
        }

        // Publish fold event
        _ = Task.Run(async () => await _eventPublisher.PublishAsync(new CardPlayedEvent
        {
            GameId = gameId,
            PlayerSeat = playerSeat,
            Card = new Card { Value = "0", Suit = "" },
            IsFold = true
        }));
    }
    else
    {
        var playSuccess = _gameFlowService.PlayCard(game, playerSeat, cardIndex);
        if (!playSuccess)
        {
            return MappingService.MapGameStateToPlayCardResponse(game, requestingPlayerSeat, _devMode, false, "Invalid card play");
        }

        // Publish card played event
        var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == playerSeat);
        if (playedCard?.Card != null)
        {
            _ = Task.Run(async () => await _eventPublisher.PublishAsync(new CardPlayedEvent
            {
                GameId = gameId,
                PlayerSeat = playerSeat,
                Card = playedCard.Card,
                IsFold = false
            }));
        }
    }

    // ... rest of existing logic
}
```

### Step 1.6: Test Event System

**Create**: `TrucoMineiro.Tests/EventSystemTests.cs`
```csharp
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.EventHandlers;
using TrucoMineiro.API.Domain.Services;
using Xunit;

namespace TrucoMineiro.Tests
{
    public class EventSystemTests
    {
        [Fact]
        public async Task EventPublisher_ShouldPublishEvents_ToRegisteredHandlers()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddScoped<IEventPublisher, InMemoryEventPublisher>();
            services.AddScoped<IEventHandler<CardPlayedEvent>, GameLoggingEventHandler>();
            
            var serviceProvider = services.BuildServiceProvider();
            var eventPublisher = serviceProvider.GetRequiredService<IEventPublisher>();

            var cardEvent = new CardPlayedEvent
            {
                GameId = "test-game",
                PlayerSeat = 0,
                Card = new TrucoMineiro.API.Domain.Models.Card { Value = "A", Suit = "Hearts" },
                IsFold = false
            };

            // Act & Assert (should not throw)
            await eventPublisher.PublishAsync(cardEvent);
        }
    }
}
```

---

## Phase 2: Event-Driven AI

### Step 2.1: Create AI Event Handler

**Create**: `TrucoMineiro.API/Domain/EventHandlers/AIPlayerEventHandler.cs`
```csharp
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    public class AIPlayerEventHandler : IEventHandler<PlayerTurnStartedEvent>
    {
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ILogger<AIPlayerEventHandler> _logger;

        public AIPlayerEventHandler(
            IAIPlayerService aiPlayerService,
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            ILogger<AIPlayerEventHandler> logger)
        {
            _aiPlayerService = aiPlayerService;
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _logger = logger;
        }

        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId);
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for AI turn", gameEvent.GameId);
                    return;
                }

                var player = game.Players.FirstOrDefault(p => p.Seat == gameEvent.PlayerSeat);
                if (player == null || !player.IsAI)
                {
                    // Not an AI player, ignore
                    return;
                }

                _logger.LogDebug("AI player {PlayerSeat} thinking in game {GameId}", gameEvent.PlayerSeat, gameEvent.GameId);

                // Add thinking delay for realism
                await Task.Delay(GetAIThinkingDelay(), cancellationToken);

                // AI makes decision
                var cardIndex = _aiPlayerService.SelectCardToPlay(player, game);
                
                if (cardIndex >= 0 && cardIndex < player.Hand.Count)
                {
                    var card = player.Hand[cardIndex];
                    player.Hand.RemoveAt(cardIndex);

                    // Update game state
                    var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
                    if (playedCard != null)
                    {
                        playedCard.Card = card;
                    }
                    else
                    {
                        game.PlayedCards.Add(new PlayedCard(player.Seat, card));
                    }

                    // Save game state
                    await _gameRepository.SaveGameAsync(game);

                    // Publish card played event
                    await _eventPublisher.PublishAsync(new CardPlayedEvent
                    {
                        GameId = gameEvent.GameId,
                        PlayerSeat = gameEvent.PlayerSeat,
                        Card = card,
                        IsFold = false
                    }, cancellationToken);

                    _logger.LogDebug("AI player {PlayerSeat} played {Card} in game {GameId}", 
                        gameEvent.PlayerSeat, $"{card.Value} of {card.Suit}", gameEvent.GameId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI turn for player {PlayerSeat} in game {GameId}", 
                    gameEvent.PlayerSeat, gameEvent.GameId);
            }
        }

        private TimeSpan GetAIThinkingDelay()
        {
            // Random delay between 500ms and 2000ms for realism
            var random = new Random();
            return TimeSpan.FromMilliseconds(random.Next(500, 2000));
        }
    }
}
```

### Step 2.2: Create Game Flow Event Handler

**Create**: `TrucoMineiro.API/Domain/EventHandlers/GameFlowEventHandler.cs`
```csharp
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    public class GameFlowEventHandler : IEventHandler<CardPlayedEvent>
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IGameFlowService _gameFlowService;
        private readonly ILogger<GameFlowEventHandler> _logger;

        public GameFlowEventHandler(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IGameFlowService gameFlowService,
            ILogger<GameFlowEventHandler> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _gameFlowService = gameFlowService;
            _logger = logger;
        }

        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                var game = await _gameRepository.GetGameAsync(gameEvent.GameId);
                if (game == null) return;

                // Advance to next player
                _gameFlowService.AdvanceToNextPlayer(game);

                // Check if round is complete
                if (_gameFlowService.IsRoundComplete(game))
                {
                    // Determine round winner
                    var winner = DetermineRoundWinner(game);
                    
                    await _eventPublisher.PublishAsync(new RoundCompletedEvent
                    {
                        GameId = gameEvent.GameId,
                        WinningSeat = winner,
                        PlayedCards = new List<PlayedCard>(game.PlayedCards)
                    }, cancellationToken);

                    // Clear played cards for next round
                    foreach (var pc in game.PlayedCards)
                    {
                        pc.Card = null;
                    }

                    // Check if hand is complete
                    if (game.Players.All(p => p.Hand.Count == 0))
                    {
                        await _eventPublisher.PublishAsync(new HandCompletedEvent
                        {
                            GameId = gameEvent.GameId,
                            WinningTeam = winner % 2, // Simple team calculation
                            PointsAwarded = 1
                        }, cancellationToken);
                    }
                    else
                    {
                        // Start next round - first player from winning team plays
                        var nextPlayer = winner;
                        game.Players.ForEach(p => p.IsActive = false);
                        game.Players[nextPlayer].IsActive = true;

                        await _eventPublisher.PublishAsync(new PlayerTurnStartedEvent
                        {
                            GameId = gameEvent.GameId,
                            PlayerSeat = nextPlayer
                        }, cancellationToken);
                    }
                }
                else
                {
                    // Continue with next player
                    var activePlayer = game.Players.FirstOrDefault(p => p.IsActive);
                    if (activePlayer != null)
                    {
                        await _eventPublisher.PublishAsync(new PlayerTurnStartedEvent
                        {
                            GameId = gameEvent.GameId,
                            PlayerSeat = activePlayer.Seat
                        }, cancellationToken);
                    }
                }

                await _gameRepository.SaveGameAsync(game);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing game flow for card played event in game {GameId}", gameEvent.GameId);
            }
        }

        private int DetermineRoundWinner(GameState game)
        {
            // Simplified winner determination - use existing TrucoRulesEngine logic
            // This should be moved to a proper service
            var playedCards = game.PlayedCards.Where(pc => pc.Card != null).ToList();
            if (!playedCards.Any()) return 0;

            // Simple implementation - highest card wins
            var winner = playedCards.OrderByDescending(pc => GetCardValue(pc.Card!)).First();
            return winner.PlayerSeat;
        }

        private int GetCardValue(Card card)
        {
            // Simplified card value calculation
            return card.Value switch
            {
                "3" => 10,
                "2" => 9,
                "A" => 8,
                "K" => 7,
                "J" => 6,
                "Q" => 5,
                "7" => 4,
                "6" => 3,
                "5" => 2,
                "4" => 1,
                _ => 0
            };
        }
    }
}
```

### Step 2.3: Update Dependency Injection for AI Events

**Modify**: `TrucoMineiro.API/Program.cs`
```csharp
// Add AI event handlers
builder.Services.AddScoped<IEventHandler<PlayerTurnStartedEvent>, AIPlayerEventHandler>();
builder.Services.AddScoped<IEventHandler<CardPlayedEvent>, GameFlowEventHandler>();
```

### Step 2.4: Create Event-Driven Game Start

**Modify**: `TrucoMineiro.API/Domain/Services/GameService.cs`
```csharp
// Add to CreateGame method
public GameState CreateGame(string? playerName = null)
{
    // ... existing logic

    // Publish game started event and first player turn
    _ = Task.Run(async () =>
    {
        await _eventPublisher.PublishAsync(new PlayerTurnStartedEvent
        {
            GameId = gameState.GameId,
            PlayerSeat = gameState.FirstPlayerSeat
        });
    });

    return gameState;
}
```

---

## Phase 3: Game State Machine

### Step 3.1: Create Game Commands

**Create**: `TrucoMineiro.API/Domain/Commands/GameCommands.cs`
```csharp
namespace TrucoMineiro.API.Domain.Commands
{
    public abstract class GameCommand
    {
        public string GameId { get; set; } = string.Empty;
        public int PlayerSeat { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public abstract string CommandType { get; }
    }

    public class PlayCardCommand : GameCommand
    {
        public override string CommandType => "PlayCard";
        public int CardIndex { get; set; }
        public bool IsFold { get; set; }
    }

    public class CallTrucoCommand : GameCommand
    {
        public override string CommandType => "CallTruco";
    }

    public class RespondToTrucoCommand : GameCommand
    {
        public override string CommandType => "RespondToTruco";
        public TrucoResponse Response { get; set; }
    }
}
```

### Step 3.2: Create Game State Machine

**Create**: `TrucoMineiro.API/Domain/Services/GameStateMachine.cs`
```csharp
using TrucoMineiro.API.Domain.Commands;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Domain.Services
{
    public enum GamePhase
    {
        WaitingForPlayers,
        HandStarted,
        WaitingForCardPlay,
        WaitingForTrucoResponse,
        RoundComplete,
        HandComplete,
        GameComplete
    }

    public interface IGameStateMachine
    {
        Task<bool> ProcessCommandAsync(GameCommand command);
        Task<GameState?> GetGameStateAsync(string gameId);
    }

    public class GameStateMachine : IGameStateMachine
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly ITrucoRulesEngine _trucoRulesEngine;

        public GameStateMachine(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            ITrucoRulesEngine trucoRulesEngine)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _trucoRulesEngine = trucoRulesEngine;
        }

        public async Task<bool> ProcessCommandAsync(GameCommand command)
        {
            var game = await _gameRepository.GetGameAsync(command.GameId);
            if (game == null) return false;

            return command switch
            {
                PlayCardCommand playCard => await ProcessPlayCardCommand(game, playCard),
                CallTrucoCommand callTruco => await ProcessCallTrucoCommand(game, callTruco),
                RespondToTrucoCommand respondTruco => await ProcessRespondToTrucoCommand(game, respondTruco),
                _ => false
            };
        }

        public async Task<GameState?> GetGameStateAsync(string gameId)
        {
            return await _gameRepository.GetGameAsync(gameId);
        }

        private async Task<bool> ProcessPlayCardCommand(GameState game, PlayCardCommand command)
        {
            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null || !player.IsActive) return false;

            if (command.IsFold)
            {
                // Handle fold
                var foldCard = new Card { Value = "0", Suit = "" };
                var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
                if (playedCard != null)
                {
                    playedCard.Card = foldCard;
                }
                else
                {
                    game.PlayedCards.Add(new PlayedCard(player.Seat, foldCard));
                }

                await _eventPublisher.PublishAsync(new CardPlayedEvent
                {
                    GameId = command.GameId,
                    PlayerSeat = command.PlayerSeat,
                    Card = foldCard,
                    IsFold = true
                });
            }
            else
            {
                // Handle regular card play
                if (command.CardIndex < 0 || command.CardIndex >= player.Hand.Count) return false;

                var card = player.Hand[command.CardIndex];
                player.Hand.RemoveAt(command.CardIndex);

                var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
                if (playedCard != null)
                {
                    playedCard.Card = card;
                }
                else
                {
                    game.PlayedCards.Add(new PlayedCard(player.Seat, card));
                }

                await _eventPublisher.PublishAsync(new CardPlayedEvent
                {
                    GameId = command.GameId,
                    PlayerSeat = command.PlayerSeat,
                    Card = card,
                    IsFold = false
                });
            }

            await _gameRepository.SaveGameAsync(game);
            return true;
        }

        private async Task<bool> ProcessCallTrucoCommand(GameState game, CallTrucoCommand command)
        {
            // Validate truco call
            if (game.IsTrucoCalled) return false;

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null) return false;

            // Update game state
            game.IsTrucoCalled = true;
            game.TrucoCallerSeat = command.PlayerSeat;
            game.CurrentStake *= 2; // Simple stake doubling

            // Determine who needs to respond (opposing team)
            var respondingPlayers = game.Players
                .Where(p => p.Seat % 2 != command.PlayerSeat % 2)
                .Select(p => p.Seat)
                .ToList();

            await _eventPublisher.PublishAsync(new TrucoCalledEvent
            {
                GameId = command.GameId,
                CallerSeat = command.PlayerSeat,
                NewStake = game.CurrentStake,
                RespondingPlayers = respondingPlayers
            });

            await _gameRepository.SaveGameAsync(game);
            return true;
        }

        private async Task<bool> ProcessRespondToTrucoCommand(GameState game, RespondToTrucoCommand command)
        {
            // Validate response
            if (!game.IsTrucoCalled) return false;

            await _eventPublisher.PublishAsync(new TrucoResponseEvent
            {
                GameId = command.GameId,
                ResponderSeat = command.PlayerSeat,
                Response = command.Response,
                NewStake = command.Response == TrucoResponse.Raise ? game.CurrentStake * 2 : null
            });

            await _gameRepository.SaveGameAsync(game);
            return true;
        }
    }
}
```

### Step 3.3: Update Controller to Use State Machine

**Modify**: `TrucoMineiro.API/Controllers/TrucoGameController.cs`
```csharp
// Add state machine dependency
private readonly IGameStateMachine _gameStateMachine;

// Modify PlayCard endpoint
[HttpPost("play-card")]
public async Task<ActionResult<PlayCardResponseDto>> PlayCard([FromBody] PlayCardRequestDto request)
{
    var command = new PlayCardCommand
    {
        GameId = request.GameId,
        PlayerSeat = request.PlayerSeat,
        CardIndex = request.CardIndex,
        IsFold = request.IsFold
    };

    var success = await _gameStateMachine.ProcessCommandAsync(command);
    if (!success)
    {
        return BadRequest("Invalid card play command");
    }

    var game = await _gameStateMachine.GetGameStateAsync(request.GameId);
    if (game == null)
    {
        return NotFound("Game not found");
    }

    var response = MappingService.MapGameStateToPlayCardResponse(game, request.PlayerSeat, _gameService.IsDevMode(), true, "Card played successfully");
    return Ok(response);
}
```

---

## Phase 4: Real-time Multiplayer

### Step 4.1: Add SignalR

**Install package**:
```bash
dotnet add TrucoMineiro.API package Microsoft.AspNetCore.SignalR
```

**Create**: `TrucoMineiro.API/Hubs/GameHub.cs`
```csharp
using Microsoft.AspNetCore.SignalR;

namespace TrucoMineiro.API.Hubs
{
    public class GameHub : Hub
    {
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Game-{gameId}");
        }

        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Game-{gameId}");
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            // Handle player disconnection
            await base.OnDisconnectedAsync(exception);
        }
    }
}
```

### Step 4.2: Create Real-time Event Handler

**Create**: `TrucoMineiro.API/Domain/EventHandlers/RealTimeNotificationHandler.cs`
```csharp
using Microsoft.AspNetCore.SignalR;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Hubs;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    public class RealTimeNotificationHandler : 
        IEventHandler<CardPlayedEvent>,
        IEventHandler<PlayerTurnStartedEvent>,
        IEventHandler<TrucoCalledEvent>,
        IEventHandler<RoundCompletedEvent>
    {
        private readonly IHubContext<GameHub> _hubContext;

        public RealTimeNotificationHandler(IHubContext<GameHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task HandleAsync(CardPlayedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"Game-{gameEvent.GameId}")
                .SendAsync("CardPlayed", new
                {
                    PlayerSeat = gameEvent.PlayerSeat,
                    Card = gameEvent.IsFold ? null : gameEvent.Card,
                    IsFold = gameEvent.IsFold,
                    Timestamp = gameEvent.Timestamp
                }, cancellationToken);
        }

        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"Game-{gameEvent.GameId}")
                .SendAsync("PlayerTurnStarted", new
                {
                    PlayerSeat = gameEvent.PlayerSeat,
                    TimeLimit = gameEvent.TimeLimit,
                    Timestamp = gameEvent.Timestamp
                }, cancellationToken);
        }

        public async Task HandleAsync(TrucoCalledEvent gameEvent, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"Game-{gameEvent.GameId}")
                .SendAsync("TrucoCalled", new
                {
                    CallerSeat = gameEvent.CallerSeat,
                    NewStake = gameEvent.NewStake,
                    RespondingPlayers = gameEvent.RespondingPlayers,
                    Timestamp = gameEvent.Timestamp
                }, cancellationToken);
        }

        public async Task HandleAsync(RoundCompletedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients.Group($"Game-{gameEvent.GameId}")
                .SendAsync("RoundCompleted", new
                {
                    WinningSeat = gameEvent.WinningSeat,
                    PlayedCards = gameEvent.PlayedCards,
                    Timestamp = gameEvent.Timestamp
                }, cancellationToken);
        }
    }
}
```

### Step 4.3: Configure SignalR in Program.cs

**Modify**: `TrucoMineiro.API/Program.cs`
```csharp
// Add SignalR
builder.Services.AddSignalR();

// Register real-time handlers
builder.Services.AddScoped<IEventHandler<CardPlayedEvent>, RealTimeNotificationHandler>();
builder.Services.AddScoped<IEventHandler<PlayerTurnStartedEvent>, RealTimeNotificationHandler>();
builder.Services.AddScoped<IEventHandler<TrucoCalledEvent>, RealTimeNotificationHandler>();
builder.Services.AddScoped<IEventHandler<RoundCompletedEvent>, RealTimeNotificationHandler>();

// Configure hub
app.MapHub<GameHub>("/gameHub");
```

---

## Phase 5: Advanced Features

### Step 5.1: Player Timeout System

**Create**: `TrucoMineiro.API/Domain/EventHandlers/PlayerTimeoutHandler.cs`
```csharp
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Commands;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Interfaces;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    public class PlayerTimeoutHandler : IEventHandler<PlayerTurnStartedEvent>
    {
        private readonly IGameStateMachine _gameStateMachine;
        private readonly ILogger<PlayerTimeoutHandler> _logger;
        private readonly Dictionary<string, CancellationTokenSource> _timeoutTokens = new();

        public PlayerTimeoutHandler(IGameStateMachine gameStateMachine, ILogger<PlayerTimeoutHandler> logger)
        {
            _gameStateMachine = gameStateMachine;
            _logger = logger;
        }

        public async Task HandleAsync(PlayerTurnStartedEvent gameEvent, CancellationToken cancellationToken = default)
        {
            var timeoutKey = $"{gameEvent.GameId}-{gameEvent.PlayerSeat}";
            
            // Cancel any existing timeout for this player
            if (_timeoutTokens.ContainsKey(timeoutKey))
            {
                _timeoutTokens[timeoutKey].Cancel();
                _timeoutTokens.Remove(timeoutKey);
            }

            // Set timeout (default 30 seconds)
            var timeoutDuration = gameEvent.TimeLimit ?? TimeSpan.FromSeconds(30);
            var timeoutCts = new CancellationTokenSource(timeoutDuration);
            _timeoutTokens[timeoutKey] = timeoutCts;

            try
            {
                await Task.Delay(timeoutDuration, timeoutCts.Token);
                
                // Timeout occurred - force fold
                _logger.LogWarning("Player {PlayerSeat} timed out in game {GameId}, forcing fold", 
                    gameEvent.PlayerSeat, gameEvent.GameId);

                var foldCommand = new PlayCardCommand
                {
                    GameId = gameEvent.GameId,
                    PlayerSeat = gameEvent.PlayerSeat,
                    CardIndex = 0,
                    IsFold = true
                };

                await _gameStateMachine.ProcessCommandAsync(foldCommand);
            }
            catch (OperationCanceledException)
            {
                // Timeout was cancelled (player acted in time)
            }
            finally
            {
                _timeoutTokens.Remove(timeoutKey);
            }
        }
    }
}
```

### Step 5.2: Game Reconnection System

**Create**: `TrucoMineiro.API/Domain/Services/PlayerConnectionManager.cs`
```csharp
using System.Collections.Concurrent;

namespace TrucoMineiro.API.Domain.Services
{
    public interface IPlayerConnectionManager
    {
        void PlayerConnected(string gameId, int playerSeat, string connectionId);
        void PlayerDisconnected(string connectionId);
        bool IsPlayerConnected(string gameId, int playerSeat);
        string? GetPlayerConnectionId(string gameId, int playerSeat);
    }

    public class PlayerConnectionManager : IPlayerConnectionManager
    {
        private readonly ConcurrentDictionary<string, (string GameId, int PlayerSeat)> _connectionToPlayer = new();
        private readonly ConcurrentDictionary<string, string> _playerToConnection = new();

        public void PlayerConnected(string gameId, int playerSeat, string connectionId)
        {
            var playerKey = $"{gameId}-{playerSeat}";
            
            // Remove old connection if exists
            if (_playerToConnection.TryGetValue(playerKey, out var oldConnectionId))
            {
                _connectionToPlayer.TryRemove(oldConnectionId, out _);
            }

            _playerToConnection[playerKey] = connectionId;
            _connectionToPlayer[connectionId] = (gameId, playerSeat);
        }

        public void PlayerDisconnected(string connectionId)
        {
            if (_connectionToPlayer.TryRemove(connectionId, out var player))
            {
                var playerKey = $"{player.GameId}-{player.PlayerSeat}";
                _playerToConnection.TryRemove(playerKey, out _);
            }
        }

        public bool IsPlayerConnected(string gameId, int playerSeat)
        {
            var playerKey = $"{gameId}-{playerSeat}";
            return _playerToConnection.ContainsKey(playerKey);
        }

        public string? GetPlayerConnectionId(string gameId, int playerSeat)
        {
            var playerKey = $"{gameId}-{playerSeat}";
            return _playerToConnection.TryGetValue(playerKey, out var connectionId) ? connectionId : null;
        }
    }
}
```

---

## Testing Strategy

### Unit Tests for Each Phase

1. **Phase 1**: Event system tests
2. **Phase 2**: AI event handler tests
3. **Phase 3**: State machine command tests
4. **Phase 4**: SignalR integration tests
5. **Phase 5**: Timeout and reconnection tests

### Integration Tests

1. **Full game flow** with events
2. **Mixed human/AI games**
3. **Multiplayer scenarios**
4. **Disconnection/reconnection**

---

## Rollback Plan

Each phase includes:
1. **Feature flags** to switch between old and new systems
2. **Gradual migration** with parallel systems
3. **Comprehensive testing** before removing old code
4. **Database compatibility** maintained throughout

---

## Success Metrics

### Functional
- ✅ All existing functionality preserved
- ✅ AI players work identically to before
- ✅ Game rules and flow maintained
- ✅ Performance not degraded

### Technical
- ✅ Event-driven architecture implemented
- ✅ Real-time multiplayer support
- ✅ Scalable to multiple game rooms
- ✅ Testable and maintainable code

### Future-Ready
- ✅ Easy to add new game features
- ✅ Support for spectators
- ✅ Game replay capability
- ✅ Analytics and monitoring ready

---

## Execution Timeline

| Phase | Duration | Effort | Risk |
|-------|----------|--------|------|
| Phase 1: Foundation | 1-2 days | Medium | Low |
| Phase 2: Event-Driven AI | 2-3 days | High | Medium |
| Phase 3: State Machine | 3-4 days | High | High |
| Phase 4: Real-time | 2-3 days | Medium | Medium |
| Phase 5: Advanced | 2-3 days | Medium | Low |
| **Total** | **10-15 days** | **High** | **Medium** |

---

## Ready to Start?

This plan provides a comprehensive roadmap to transform the Truco game into a scalable, event-driven, multiplayer-ready system while maintaining all existing functionality.

**Recommended Starting Point**: Begin with **Phase 1** to establish the event system foundation, then proceed incrementally through each phase.

Each phase can be developed, tested, and deployed independently, minimizing risk and ensuring continuous functionality throughout the migration.
