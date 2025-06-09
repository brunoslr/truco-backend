# Event-Driven Architecture Migration - Execution Plan

## Overview
This document provides a step-by-step execution plan for migrating from the current synchronous architecture to an event-driven system. Each step includes detailed implementation instructions, code examples, and validation criteria.

## Pre-Migration Checklist
- [x] Build succeeds with no errors
- [x] All tests pass
- [x] Migration plan documented
- [ ] Backup current working state
- [ ] Create feature branch

## Phase 1: Foundation - Event System Infrastructure
**Estimated Time**: 1-2 days  
**Risk Level**: Low  
**Status**: ✅ COMPLETED

### Step 1.1: Create Event Base Infrastructure
**Goal**: Establish the foundation for all events and event handling
**Status**: ✅ COMPLETED

#### 1.1.1 Create Event Interfaces and Base Classes
**Status**: ✅ COMPLETED

#### 1.1.2 Create Event Publisher Implementation
**Status**: ✅ COMPLETED

#### 1.1.3 Register Services in DI Container
**Status**: ✅ COMPLETED

**Validation Criteria:**
- [x] Build succeeds
- [x] Event interfaces are accessible
- [x] DI registration works
- [x] Can create and publish test events

### Step 1.2: Create Core Game Events
**Goal**: Define the essential events that will drive the game flow
**Status**: ✅ COMPLETED

#### 1.2.1 Create Game Events
**Status**: ✅ COMPLETED

**Validation Criteria:**
- [x] All events inherit from GameEventBase
- [x] Events contain necessary game state information
- [x] Events can be serialized/deserialized
- [x] Build succeeds

### Step 1.3: Create Test Infrastructure
**Goal**: Ensure we can test event-driven functionality
**Status**: ✅ COMPLETED

#### 1.3.1 Create Event Testing Utilities
**Status**: ✅ COMPLETED

**Validation Criteria:**
- [x] Can publish and capture events in tests
- [x] Event handlers can be tested in isolation
- [x] All tests pass

### Step 1.4: Create ActionLog Event Handler
**Goal**: Replace direct ActionLogEntry creation with event-driven approach
**Status**: ✅ COMPLETED

#### 1.4.1 Create ActionLogEventHandler
**Status**: ✅ COMPLETED
- ActionLogEventHandler creates ActionLogEntry records from events
- Removed duplicate ActionLogEntry creation from GameService and GameFlowService
- Handler registered for CardPlayedEvent, PlayerTurnStartedEvent, and RoundCompletedEvent

**Validation Criteria:**
- [x] ActionLogEventHandler creates proper ActionLogEntry records
- [x] Duplicate ActionLogEntry creation removed from services
- [x] Frontend action log displays correctly
- [x] All tests pass

## Phase 2: Event-Driven AI Players
**Estimated Time**: 2-3 days  
**Risk Level**: Medium  
**Status**: 🔄 IN PROGRESS

### Step 2.1: Create AI Event Handlers
**Goal**: Replace synchronous AI processing with event-driven handlers
**Status**: ✅ COMPLETED

#### 2.1.1 Create AI Player Event Handler
**Status**: ✅ COMPLETED

#### 2.1.2 Modify AI Service to be Event-Driven
**Status**: ✅ COMPLETED

**Validation Criteria:**
- [x] AI responds to CardPlayedEvent
- [x] AI responds to PlayerTurnStartedEvent
- [x] AI decisions are published as events
- [x] Original AI logic is preserved
- [x] Tests pass

### Step 2.2: Integrate AI with Event System
**Goal**: Connect AI handlers to the event pipeline
**Status**: ✅ COMPLETED

#### 2.2.1 Modify Game Flow to Publish Events
**Status**: ✅ COMPLETED - GameService now publishes CardPlayedEvent

#### 2.2.2 Create AI Integration Tests
**Status**: ✅ COMPLETED - Tests already exist and pass

**Validation Criteria:**
- [x] AI players are triggered by events
- [x] AI decisions create new events
- [x] Game flow continues correctly
- [x] All existing tests still pass

## Phase 3: Game State Machine
**Estimated Time**: 3-4 days  
**Risk Level**: High  
**Status**: Pending Phase 2

### Step 3.1: Create Game State Machine Infrastructure
**Goal**: Replace synchronous game flow with command-driven state machine

#### 3.1.1 Create State Machine Components
**Files to Create:**
1. `TrucoMineiro.API/Domain/StateMachine/IGameCommand.cs`
2. `TrucoMineiro.API/Domain/StateMachine/GameCommandBase.cs`
3. `TrucoMineiro.API/Domain/StateMachine/IGameStateMachine.cs`
4. `TrucoMineiro.API/Domain/StateMachine/GameStateMachine.cs`

#### 3.1.2 Create Game Commands
**Files to Create:**
1. `TrucoMineiro.API/Domain/StateMachine/Commands/PlayCardCommand.cs`
2. `TrucoMineiro.API/Domain/StateMachine/Commands/StartGameCommand.cs`
3. `TrucoMineiro.API/Domain/StateMachine/Commands/FoldCommand.cs`

**Validation Criteria:**
- [ ] Commands can be executed
- [ ] Commands produce events
- [ ] State machine validates game state
- [ ] Build succeeds

### Step 3.2: Migrate Controllers to Use Commands
**Goal**: Replace direct service calls with command execution

#### 3.2.1 Modify Game Controller
**Files to Modify:**
- `TrucoMineiro.API/Controllers/GameController.cs`

**Validation Criteria:**
- [ ] Controllers execute commands instead of calling services directly
- [ ] API endpoints continue to work
- [ ] Response times are acceptable
- [ ] All tests pass

## Phase 4: Real-time Multiplayer with SignalR
**Estimated Time**: 2-3 days  
**Risk Level**: Medium  
**Status**: Pending Phase 3

### Step 4.1: Add SignalR Infrastructure
**Goal**: Enable real-time notifications for multiplayer

#### 4.1.1 Install and Configure SignalR
**Packages to Install:**
- Microsoft.AspNetCore.SignalR

#### 4.1.2 Create Game Hub
**Files to Create:**
1. `TrucoMineiro.API/Hubs/GameHub.cs`
2. `TrucoMineiro.API/Domain/EventHandlers/SignalREventHandler.cs`

**Validation Criteria:**
- [ ] SignalR hub accepts connections
- [ ] Events are broadcast to connected clients
- [ ] Client can receive game updates
- [ ] Build succeeds

### Step 4.2: Create Client Integration
**Goal**: Provide real-time updates to game clients

#### 4.2.1 Add SignalR Event Broadcasting
**Files to Modify:**
- `TrucoMineiro.API/Domain/Services/InMemoryEventPublisher.cs`

**Validation Criteria:**
- [ ] Game events trigger SignalR notifications
- [ ] Multiple clients can connect to same game
- [ ] Events are sent to correct game participants
- [ ] Performance is acceptable

## Phase 5: Advanced Features
**Estimated Time**: 2-3 days  
**Risk Level**: Low  
**Status**: Pending Phase 4

### Step 5.1: Add Game Management Features
**Goal**: Implement timeouts, reconnections, and spectators

#### 5.1.1 Player Timeout Handling
**Files to Create:**
1. `TrucoMineiro.API/Domain/EventHandlers/TimeoutEventHandler.cs`
2. `TrucoMineiro.API/Domain/Events/GameEvents/PlayerTimeoutEvent.cs`

#### 5.1.2 Reconnection Support
**Files to Create:**
1. `TrucoMineiro.API/Domain/Services/GameStateSnapshotService.cs`

#### 5.1.3 Spectator Mode
**Files to Modify:**
- `TrucoMineiro.API/Hubs/GameHub.cs`

**Validation Criteria:**
- [ ] Players can timeout and be auto-played
- [ ] Players can reconnect and resume
- [ ] Spectators can watch games
- [ ] All features work with existing game flow

## Implementation Guidelines

### Code Standards
- Follow existing naming conventions
- Add comprehensive XML documentation
- Include unit tests for all new components
- Maintain backward compatibility during migration
- Use async/await consistently

### Testing Strategy
- Test each phase independently
- Maintain green builds throughout migration
- Add integration tests for event flows
- Performance test event publishing under load

### Rollback Plan
Each phase should be implemented in a way that allows rollback:
1. Use feature flags to toggle between old and new systems
2. Keep old code until migration is complete and tested
3. Have database migration scripts ready
4. Maintain API compatibility

### Monitoring and Logging
- Log all events with correlation IDs
- Monitor event processing times
- Track error rates in event handlers
- Add health checks for event system

## Success Criteria

### Phase 1 Success
- [ ] Event system publishes and handles events
- [ ] No regression in existing functionality
- [ ] All tests pass
- [ ] Build pipeline succeeds

### Phase 2 Success
- [ ] AI players respond to events asynchronously
- [ ] Game flow continues to work correctly
- [ ] Performance is maintained or improved
- [ ] All tests pass

### Phase 3 Success
- [ ] Game logic runs through state machine
- [ ] Controllers use command pattern
- [ ] Game state is consistent
- [ ] All tests pass

### Phase 4 Success
- [ ] Real-time notifications work
- [ ] Multiple players can play simultaneously
- [ ] SignalR connections are stable
- [ ] All tests pass

### Phase 5 Success
- [ ] Advanced features work correctly
- [ ] System is ready for production multiplayer
- [ ] Performance meets requirements
- [ ] All tests pass

## Next Steps

1. **Create backup and feature branch**
2. **Start Phase 1: Event System Infrastructure**
3. **Validate each step before proceeding**
4. **Maintain communication with stakeholders**
5. **Document any deviations from the plan**

## Risk Mitigation

- Implement behind feature flags
- Maintain parallel systems during transition
- Have rollback procedures ready
- Test thoroughly at each phase
- Get stakeholder approval before high-risk phases

---

**Note**: This plan is living document. Update it as implementation progresses and requirements evolve.
