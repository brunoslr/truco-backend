using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.StateMachine.Commands;

namespace TrucoMineiro.API.Domain.StateMachine
{
    /// <summary>
    /// Game state machine that processes commands and manages game flow
    /// </summary>
    public class GameStateMachine : IGameStateMachine
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IHandResolutionService _handResolutionService;
        private readonly ILogger<GameStateMachine> _logger;

        public GameStateMachine(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IAIPlayerService aiPlayerService,
            IHandResolutionService handResolutionService,
            ILogger<GameStateMachine> logger)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _aiPlayerService = aiPlayerService;
            _handResolutionService = handResolutionService;
            _logger = logger;
        }        public async Task<CommandResult> ProcessCommandAsync(IGameCommand command)
        {
            try
            {
                if (command == null)
                {
                    return CommandResult.Failure("Command cannot be null");
                }

                _logger.LogInformation("Processing command {CommandType} for game {GameId}", 
                    command.CommandType, command.GameId);

                // Validate command
                var validationResult = await ValidateCommandAsync(command);
                if (!validationResult.IsSuccess)
                {
                    return validationResult;
                }

                // Get current game state
                var game = await _gameRepository.GetGameAsync(command.GameId.ToString());
                if (game == null)
                {
                    return CommandResult.Failure($"Game {command.GameId} not found");
                }                // Process command based on type
                var result = command switch
                {
                    StartGameCommand startCmd => await ProcessStartGameCommand(startCmd, game),
                    // REMOVED: PlayCardCommand - moved to PlayCardService for consolidation
                    CallTrucoCommand trucoCmd => await ProcessCallTrucoCommand(trucoCmd, game),
                    RespondToTrucoCommand respondCmd => await ProcessRespondToTrucoCommand(respondCmd, game),
                    FoldCommand foldCmd => await ProcessFoldCommand(foldCmd, game),
                    _ => CommandResult.Failure($"Unknown command type: {command.CommandType}")
                };

                // Save game state if command was successful
                if (result.IsSuccess)
                {
                    await _gameRepository.SaveGameAsync(game);
                    _logger.LogInformation("Command {CommandType} processed successfully for game {GameId}", 
                        command.CommandType, command.GameId);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing command {CommandType} for game {GameId}", 
                    command.CommandType, command.GameId);
                return CommandResult.Failure($"Internal error: {ex.Message}");
            }
        }

        public async Task<GameState?> GetGameStateAsync(Guid gameId)
        {
            return await _gameRepository.GetGameAsync(gameId.ToString());
        }

        public async Task<CommandResult> ValidateCommandAsync(IGameCommand command)
        {
            // Get current game state
            var game = await _gameRepository.GetGameAsync(command.GameId.ToString());
            if (game == null)
            {
                return CommandResult.Failure($"Game {command.GameId} not found");
            }

            // Validate based on command type and game state
            return command switch
            {
                StartGameCommand => ValidateStartGameCommand(game),
                PlayCardCommand playCmd => ValidatePlayCardCommand(playCmd, game),
                CallTrucoCommand trucoCmd => ValidateCallTrucoCommand(trucoCmd, game),
                RespondToTrucoCommand respondCmd => ValidateRespondToTrucoCommand(respondCmd, game),
                FoldCommand foldCmd => ValidateFoldCommand(foldCmd, game),
                _ => CommandResult.Failure($"Unknown command type: {command.CommandType}")
            };
        }

        private async Task<CommandResult> ProcessStartGameCommand(StartGameCommand command, GameState game)
        {
            try
            {
                // Initialize game if not already started
                if (game.GameStatus == "waiting")
                {
                    game.GameStatus = "active";
                    game.StartGame();                    // Publish game started event
                    if (Guid.TryParse(command.GameId, out var gameGuid))
                    {
                        await _eventPublisher.PublishAsync(new GameStartedEvent(
                            gameGuid,
                            game,
                            game.Players,
                            null, // startedBy player
                            null  // gameConfiguration
                        ));

                        // Start first player's turn
                        var firstPlayer = game.GetCurrentPlayer();
                        if (firstPlayer != null)
                        {
                            await _eventPublisher.PublishAsync(new PlayerTurnStartedEvent(
                                gameGuid,
                                firstPlayer,
                                game.CurrentRound,
                                game.CurrentHand,
                                game,
                                GetAvailableActions(firstPlayer, game)
                            ));
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse GameId '{GameId}' for GameStartedEvent", command.GameId);
                    }
                }

                return CommandResult.Success("Game started successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to start game: {ex.Message}");
            }
        }        // REMOVED: ProcessPlayCardCommand - PlayCard logic moved to PlayCardService
        // This reduces complexity and consolidates card play logic in a single location

        private async Task<CommandResult> ProcessCallTrucoCommand(CallTrucoCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }

                // Update game state for Truco call
                game.TrucoLevel++;
                game.TrucoCalledBy = player.Id;
                game.WaitingForTrucoResponse = true;                // Publish Truco called event
                if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new TrucoCalledEvent(
                        gameGuid,
                        player,
                        game.TrucoLevel,
                        game
                    ));
                }
                else
                {
                    _logger.LogWarning("Failed to parse GameId '{GameId}' for TrucoCalledEvent", command.GameId);
                }

                return CommandResult.Success("Truco called successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to call Truco: {ex.Message}");
            }
        }

        private async Task<CommandResult> ProcessRespondToTrucoCommand(RespondToTrucoCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }

                game.WaitingForTrucoResponse = false;                if (command.Accept)
                {
                    // Truco accepted, continue game with higher stakes
                    if (Guid.TryParse(command.GameId, out var gameGuid))
                    {
                        await _eventPublisher.PublishAsync(new TrucoAcceptedEvent(
                            gameGuid,
                            player,
                            game.TrucoLevel,
                            game
                        ));
                    }
                    else
                    {
                        _logger.LogWarning("Failed to parse GameId '{GameId}' for TrucoAcceptedEvent", command.GameId);
                    }
                }
                else
                {
                    // Truco rejected, calling team wins the hand
                    var callingPlayer = game.Players.FirstOrDefault(p => p.Id == game.TrucoCalledBy);
                    if (callingPlayer != null && Guid.TryParse(command.GameId, out var gameGuid))
                    {
                        await _eventPublisher.PublishAsync(new TrucoRejectedEvent(
                            gameGuid,
                            player,
                            callingPlayer,
                            game.TrucoLevel - 1, // Points awarded for rejection
                            game
                        ));
                    }
                    else if (callingPlayer != null)
                    {
                        _logger.LogWarning("Failed to parse GameId '{GameId}' for TrucoRejectedEvent", command.GameId);
                    }
                }

                return CommandResult.Success("Truco response processed successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to respond to Truco: {ex.Message}");
            }
        }

        private async Task<CommandResult> ProcessFoldCommand(FoldCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }

                // Mark player as folded
                player.HasFolded = true;
                game.GameStatus = "completed";                // Determine winning team (opponent team wins)
                var winningTeam = player.Team == "team1" ? "team2" : "team1";
                
                if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new PlayerFoldedEvent(
                        gameGuid,
                        player,
                        winningTeam,
                        game
                    ));
                }
                else
                {
                    _logger.LogWarning("Failed to parse GameId '{GameId}' for PlayerFoldedEvent", command.GameId);
                }

                return CommandResult.Success("Player folded successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to process fold: {ex.Message}");
            }
        }

        private CommandResult ValidateStartGameCommand(GameState game)
        {
            if (game.GameStatus != "waiting")
            {
                return CommandResult.Failure("Game has already started");
            }

            if (game.Players.Count < 2)
            {
                return CommandResult.Failure("Not enough players to start the game");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidatePlayCardCommand(PlayCardCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            if (game.WaitingForTrucoResponse)
            {
                return CommandResult.Failure("Cannot play card while waiting for Truco response");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }            if (!player.IsActive)
            {
                return CommandResult.Failure("It's not this player's turn");
            }

            if (command.Card == null)
            {
                return CommandResult.Failure("Card is required for validation");
            }

            var card = player.Hand?.FirstOrDefault(c => c.Suit == command.Card.Suit && c.Value == command.Card.Value);
            if (card == null)
            {
                return CommandResult.Failure("Card not found in player's hand");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidateCallTrucoCommand(CallTrucoCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            if (game.WaitingForTrucoResponse)
            {
                return CommandResult.Failure("Already waiting for Truco response");
            }

            if (game.TrucoLevel >= 12) // Maximum Truco level
            {
                return CommandResult.Failure("Maximum Truco level reached");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidateRespondToTrucoCommand(RespondToTrucoCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            if (!game.WaitingForTrucoResponse)
            {
                return CommandResult.Failure("Not waiting for Truco response");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }

            // Check if player belongs to the team that should respond
            var callingPlayer = game.Players.FirstOrDefault(p => p.Id == game.TrucoCalledBy);
            if (callingPlayer != null && player.Team == callingPlayer.Team)
            {
                return CommandResult.Failure("Cannot respond to your own team's Truco call");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidateFoldCommand(FoldCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }

            if (player.HasFolded)
            {
                return CommandResult.Failure("Player has already folded");
            }

            return CommandResult.Success();
        }

        private List<string> GetAvailableActions(Player player, GameState game)
        {
            var actions = new List<string>();

            if (game.GameStatus == "active")
            {
                if (!game.WaitingForTrucoResponse)
                {
                    actions.Add("play-card");
                    
                    if (game.TrucoLevel < 12)
                    {
                        actions.Add("call-truco");
                    }
                    
                    actions.Add("fold");
                }
                else
                {
                    // Check if this player should respond to Truco
                    var callingPlayer = game.Players.FirstOrDefault(p => p.Id == game.TrucoCalledBy);
                    if (callingPlayer != null && player.Team != callingPlayer.Team)
                    {
                        actions.Add("accept-truco");
                        actions.Add("reject-truco");
                    }
                }
            }

            return actions;
        }
    }
}
