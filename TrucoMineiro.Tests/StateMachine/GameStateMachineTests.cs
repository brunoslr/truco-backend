using Microsoft.Extensions.Logging;
using Moq;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.StateMachine;
using TrucoMineiro.API.Domain.StateMachine.Commands;
using Xunit;

namespace TrucoMineiro.Tests.StateMachine;

public class GameStateMachineTests
{
    private readonly Mock<IGameRepository> _mockGameRepository;
    private readonly Mock<IEventPublisher> _mockEventPublisher;
    private readonly Mock<IAIPlayerService> _mockAIPlayerService;
    private readonly Mock<IHandResolutionService> _mockHandResolutionService;
    private readonly Mock<ILogger<GameStateMachine>> _mockLogger;
    private readonly GameStateMachine _gameStateMachine;

    public GameStateMachineTests()
    {
        _mockGameRepository = new Mock<IGameRepository>();
        _mockEventPublisher = new Mock<IEventPublisher>();
        _mockAIPlayerService = new Mock<IAIPlayerService>();
        _mockHandResolutionService = new Mock<IHandResolutionService>();
        _mockLogger = new Mock<ILogger<GameStateMachine>>();
        
        _gameStateMachine = new GameStateMachine(
            _mockGameRepository.Object,
            _mockEventPublisher.Object,
            _mockAIPlayerService.Object,
            _mockHandResolutionService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ProcessCommandAsync_WithNullCommand_ShouldReturnFailure()
    {
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(null!);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Command cannot be null", result.ErrorMessage);
    }    [Fact]
    public async Task ProcessCommandAsync_WithNonExistentGame_ShouldReturnFailure()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var command = new StartGameCommand
        {
            GameId = gameId,
            PlayerSeat = 1
        };
          _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync((GameState?)null);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal($"Game {gameId} not found", result.ErrorMessage);
    }    [Fact]
    public async Task ProcessCommandAsync_StartGameCommand_ShouldStartGameAndPublishEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var command = new StartGameCommand
        {
            GameId = gameId,
            PlayerSeat = 1
        };
          var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "waiting"
        };
        
        // Initialize the game with players
        gameState.InitializeGame("TestPlayer");
        gameState.GameStatus = "waiting"; // Reset status back to waiting for the test
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("active", gameState.GameStatus);
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<GameStartedEvent>()), Times.Once);
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<PlayerTurnStartedEvent>()), Times.Once);    }

    [Fact]
    public async Task ProcessCommandAsync_PlayCardCommand_WithValidCard_ShouldPlayCardAndPublishEvent()
    {
        // Arrange        
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var card = new Card("A", "hearts");
        
        var command = new PlayCardCommand
        {
            GameId = gameId,
            PlayerSeat = 1,
            Card = card
        };
        
        var player = new Player("Test Player", Team.PlayerTeam, 1)
        {
            Id = playerId,
            Hand = new List<Card> { card },
            IsActive = true // Player must be active to play
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player }
        };
            _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
          
        // Assert
        Assert.True(result.IsSuccess);
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<CardPlayedEvent>()), Times.Once);
    }[Fact]
    public async Task ProcessCommandAsync_PlayCardCommand_WithInvalidCard_ShouldReturnFailure()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var card = new Card("A", "hearts");
        var invalidCard = new Card("2", "spades");
        
        var command = new PlayCardCommand
        {
            GameId = gameId,
            PlayerSeat = 1,
            Card = invalidCard
        };
        
        var player = new Player("Test Player", Team.PlayerTeam, 1)
        {
            Id = playerId,
            Hand = new List<Card> { card }, // Player doesn't have the invalid card
            IsActive = true // Player must be active for validation to continue
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player }
        };
          _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Card not found in player's hand", result.ErrorMessage);
    }    [Fact]
    public async Task ProcessCommandAsync_CallTrucoOrRaiseCommand_ShouldPublishTrucoOrRaiseCalledEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        
        var command = new CallTrucoOrRaiseCommand(gameId, 1);
        
        var player = new Player("Test Player", Team.PlayerTeam, 1)
        {
            Id = playerId
        };
          var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player },
            TrucoCallState = TrucoCallState.None // Can call truco
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
          // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(TrucoCallState.Truco, gameState.TrucoCallState);
        Assert.Equal((int)player.Team, gameState.LastTrucoCallerTeam);
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TrucoOrRaiseCalledEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_AcceptTrucoCommand_ShouldPublishTrucoAcceptedEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var trucoCallerId = Guid.NewGuid();
        
        var command = new AcceptTrucoCommand(gameId, 2);
        
        var player = new Player("Test Player", Team.OpponentTeam, 2)
        {
            Id = playerId
        };
          var callingPlayer = new Player("Calling Player", Team.PlayerTeam, 1)
        {
            Id = trucoCallerId
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { callingPlayer, player }, // Include both players
            TrucoCallState = TrucoCallState.Truco, // There's a pending truco call
            LastTrucoCallerTeam = (int)callingPlayer.Team, // Calling player's team made the call
            Stakes = 2 // Current stakes before confirmation
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
          // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(4, gameState.Stakes); // Stakes confirmed at Truco level
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TrucoAcceptedEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_SurrenderTrucoCommand_ShouldPublishTrucoSurrenderedEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var trucoCallerId = Guid.NewGuid();
        
        var command = new SurrenderTrucoCommand(gameId, 2);
        
        var respondingPlayer = new Player("Test Player", Team.OpponentTeam, 2)
        {
            Id = playerId
        };
        
        var callingPlayer = new Player("Calling Player", Team.PlayerTeam, 1)
        {
            Id = trucoCallerId
        };
          var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { callingPlayer, respondingPlayer }, // Include both players
            TrucoCallState = TrucoCallState.Truco, // There's a pending truco call
            LastTrucoCallerTeam = (int)callingPlayer.Team, // Calling player's team made the call
            Stakes = 4 // Current potential stakes for Truco
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
          // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("active", gameState.GameStatus); // Game remains active after Truco surrender
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TrucoSurrenderedEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_SurrenderHandCommand_ShouldMarkPlayerAsFoldedAndPublishEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();        var command = new SurrenderHandCommand(gameId, 1);
        
        var player = new Player("Test Player", Team.PlayerTeam, 1)
        {
            Id = playerId
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player }
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(player.HasFolded);
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<PlayerFoldedEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_WrongPlayerTurn_ShouldReturnFailure()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var card = new Card("A", "hearts");
          var command = new PlayCardCommand
        {
            GameId = gameId,
            PlayerSeat = 2, // Wrong turn - current player is seat 1
            Card = card
        };
        
        var player = new Player("Test Player", Team.OpponentTeam, 2)
        {
            Id = playerId,
            Hand = new List<Card> { card }
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player }
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("It's not this player's turn", result.ErrorMessage);
    }    [Fact]
    public async Task ProcessCommandAsync_GameNotInProgress_ShouldReturnFailure()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var card = new Card("A", "hearts");
        
        var command = new PlayCardCommand
        {
            GameId = gameId,
            PlayerSeat = 1,
            Card = card
        };
          var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "completed" // Game is finished
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Game is not active", result.ErrorMessage);
    }
}
