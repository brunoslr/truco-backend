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
        
        var player = new Player("Test Player", "team1", 1)
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
        
        var player = new Player("Test Player", "team1", 1)
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
    public async Task ProcessCommandAsync_CallTrucoCommand_ShouldPublishTrucoCalledEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        
        var command = new CallTrucoCommand
        {
            GameId = gameId,
            PlayerSeat = 1
        };
        
        var player = new Player("Test Player", "team1", 1)
        {
            Id = playerId
        };
          var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player },
            TrucoLevel = 1 // Can call truco
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, gameState.TrucoLevel); // Truco increases level
        Assert.Equal(playerId, gameState.TrucoCalledBy);
        Assert.True(gameState.WaitingForTrucoResponse);
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TrucoCalledEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_RespondToTrucoCommand_Accept_ShouldPublishTrucoAcceptedEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var trucoCallerId = Guid.NewGuid();
          var command = new RespondToTrucoCommand
        {
            GameId = gameId,
            PlayerSeat = 2,
            Accept = true
        };
        
        var player = new Player("Test Player", "team2", 2)
        {
            Id = playerId
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { player },
            TrucoLevel = 2,
            TrucoCalledBy = trucoCallerId,
            WaitingForTrucoResponse = true
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(gameState.WaitingForTrucoResponse);
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TrucoAcceptedEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_RespondToTrucoCommand_Reject_ShouldPublishTrucoRejectedEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
        var trucoCallerId = Guid.NewGuid();
          var command = new RespondToTrucoCommand
        {
            GameId = gameId,
            PlayerSeat = 2,
            Accept = false
        };
        
        var respondingPlayer = new Player("Test Player", "team2", 2)
        {
            Id = playerId
        };
        
        var callingPlayer = new Player("Calling Player", "team1", 1)
        {
            Id = trucoCallerId
        };
        
        var gameState = new GameState
        {
            Id = gameId,
            GameStatus = "active",
            Players = new List<Player> { callingPlayer, respondingPlayer }, // Include both players
            TrucoLevel = 2,
            TrucoCalledBy = trucoCallerId,
            WaitingForTrucoResponse = true
        };
        
        _mockGameRepository.Setup(x => x.GetGameAsync(gameId))
            .ReturnsAsync(gameState);
        
        // Act
        var result = await _gameStateMachine.ProcessCommandAsync(command);
        
        // Assert
        Assert.True(result.IsSuccess);
        Assert.False(gameState.WaitingForTrucoResponse);
        Assert.Equal("active", gameState.GameStatus); // Game remains active after Truco rejection
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<TrucoRejectedEvent>()), Times.Once);
    }    [Fact]
    public async Task ProcessCommandAsync_SurrenderHandCommand_ShouldMarkPlayerAsFoldedAndPublishEvent()
    {
        // Arrange
        var gameId = Guid.NewGuid().ToString();
        var playerId = Guid.NewGuid();
          var command = new SurrenderHandCommand
        {
            GameId = gameId,
            PlayerSeat = 1
        };
        
        var player = new Player("Test Player", "team1", 1)
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
        
        _mockEventPublisher.Verify(x => x.PublishAsync(It.IsAny<SurrenderHandEvent>()), Times.Once);
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
        
        var player = new Player("Test Player", "team2", 2)
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
