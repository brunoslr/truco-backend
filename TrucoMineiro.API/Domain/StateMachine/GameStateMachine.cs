using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Constants;
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
                    PlayCardCommand playCmd => await ProcessPlayCardCommand(playCmd, game),
                    CallTrucoOrRaiseCommand trucoRaiseCmd => await ProcessCallTrucoOrRaiseCommand(trucoRaiseCmd, game),
                    AcceptTrucoCommand acceptCmd => await ProcessAcceptTrucoCommand(acceptCmd, game),
                    SurrenderTrucoCommand surrenderTrucoCmd => await ProcessSurrenderTrucoCommand(surrenderTrucoCmd, game),
                    SurrenderHandCommand surrenderCmd => await ProcessSurrenderHandCommand(surrenderCmd, game),
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
            }            // Validate based on command type and game state
            return command switch
            {
                StartGameCommand => ValidateStartGameCommand(game),
                PlayCardCommand playCmd => ValidatePlayCardCommand(playCmd, game),
                CallTrucoOrRaiseCommand trucoRaiseCmd => ValidateCallTrucoOrRaiseCommand(trucoRaiseCmd, game),
                AcceptTrucoCommand acceptCmd => ValidateAcceptTrucoCommand(acceptCmd, game),
                SurrenderTrucoCommand surrenderTrucoCmd => ValidateSurrenderTrucoCommand(surrenderTrucoCmd, game),
                SurrenderHandCommand surrenderCmd => ValidateSurrenderHandCommand(surrenderCmd, game),
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
                    game.StartGame();                    
                    // Publish game started event
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
        }

        private async Task<CommandResult> ProcessPlayCardCommand(PlayCardCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }

                // Validate player can play
                if (!player.IsActive)
                {
                    return CommandResult.Failure("It's not this player's turn");
                }

                // Find card in player's hand
                Card cardToPlay;
                int cardIndex;
                
                if (command.Card != null)
                {
                    // Find the card in the player's hand
                    cardIndex = player.Hand.FindIndex(c => c.Value == command.Card.Value && c.Suit == command.Card.Suit);
                    if (cardIndex == -1)
                    {
                        return CommandResult.Failure("Card not found in player's hand");
                    }
                    cardToPlay = command.Card;
                }
                else
                {
                    // Use CardIndex
                    if (command.CardIndex < 0 || command.CardIndex >= player.Hand.Count)
                    {
                        return CommandResult.Failure("Invalid card index");
                    }
                    cardIndex = command.CardIndex;
                    cardToPlay = player.Hand[cardIndex];
                }

                // Remove card from player's hand
                player.Hand.RemoveAt(cardIndex);

                // Add to played cards
                var existingPlayedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
                if (existingPlayedCard != null)
                {
                    existingPlayedCard.Card = cardToPlay;
                }
                else
                {
                    game.PlayedCards.Add(new PlayedCard(player.Seat, cardToPlay));
                }

                // Publish card played event
                if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new CardPlayedEvent(
                        gameGuid,
                        player.Id,
                        cardToPlay,
                        player,
                        game.CurrentRound,
                        game.CurrentHand,
                        player.IsAI,
                        game
                    ));
                }
                else
                {
                    _logger.LogWarning("Failed to parse GameId '{GameId}' for CardPlayedEvent", command.GameId);
                }

                return CommandResult.Success("Card played successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to play card: {ex.Message}");
            }
        }        private async Task<CommandResult> ProcessCallTrucoOrRaiseCommand(CallTrucoOrRaiseCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }

                var playerTeam = (int)player.Team;
                
                // Validate that the same team cannot call/raise consecutively
                if (game.LastTrucoCallerTeam == playerTeam)
                {
                    return CommandResult.Failure("Cannot call/raise consecutively. The opposing team must respond first.");
                }

                // Determine call type and stakes progression
                var previousStakes = game.Stakes;
                var callType = game.TrucoCallState switch
                {
                    TrucoCallState.None => "Truco",
                    TrucoCallState.Truco => "Seis", 
                    TrucoCallState.Seis => "Doze",
                    _ => throw new InvalidOperationException($"Cannot raise from state {game.TrucoCallState}")
                };

                var newStakes = game.TrucoCallState switch
                {
                    TrucoCallState.None => 4,   // None -> Truco = 4 points
                    TrucoCallState.Truco => 8,  // Truco -> Seis = 8 points
                    TrucoCallState.Seis => 12,  // Seis -> Doze = 12 points
                    _ => throw new InvalidOperationException($"Cannot raise from state {game.TrucoCallState}")
                };                // Update game state
                game.TrucoCallState = game.TrucoCallState switch
                {
                    TrucoCallState.None => TrucoCallState.Truco,
                    TrucoCallState.Truco => TrucoCallState.Seis,
                    TrucoCallState.Seis => TrucoCallState.Doze,
                    _ => throw new InvalidOperationException($"Cannot raise from state {game.TrucoCallState}")
                };                game.Stakes = newStakes; // Set the new stakes

                game.LastTrucoCallerTeam = playerTeam;
                
                // The opposing team can now raise (until accepted/surrendered)
                var opposingTeam = playerTeam == 0 ? 1 : 0;
                game.CanRaiseTeam = game.TrucoCallState != TrucoCallState.Doze ? opposingTeam : null;

                // Publish event
                if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new TrucoOrRaiseCalledEvent(
                        gameGuid,
                        player,
                        playerTeam,
                        callType,
                        previousStakes,
                        newStakes,
                        game
                    ));
                }
                else
                {
                    _logger.LogWarning("Failed to parse GameId '{GameId}' for TrucoOrRaiseCalledEvent", command.GameId);
                }

                return CommandResult.Success($"{callType} called successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to call truco/raise: {ex.Message}");
            }
        }        private async Task<CommandResult> ProcessAcceptTrucoCommand(AcceptTrucoCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }

                // Update stakes to confirmed value
                var confirmedStakes = game.TrucoCallState switch
                {
                    TrucoCallState.Truco => 4,
                    TrucoCallState.Seis => 8,
                    TrucoCallState.Doze => 12,
                    _ => throw new InvalidOperationException($"Cannot accept from state {game.TrucoCallState}")
                };

                game.Stakes = confirmedStakes;
                  // Set which team can raise next (accepting team)
                var acceptingTeam = (int)player.Team;
                game.CanRaiseTeam = game.TrucoCallState != TrucoCallState.Doze ? acceptingTeam : null;

                // Publish event
                if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new TrucoAcceptedEvent(
                        gameGuid,
                        player,
                        acceptingTeam,
                        confirmedStakes,
                        game.CanRaiseTeam,
                        game
                    ));
                }
                else
                {
                    _logger.LogWarning("Failed to parse GameId '{GameId}' for TrucoAcceptedEvent", command.GameId);
                }

                return CommandResult.Success("Truco accepted successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to accept Truco: {ex.Message}");
            }
        }

        private async Task<CommandResult> ProcessSurrenderTrucoCommand(SurrenderTrucoCommand command, GameState game)
        {
            try
            {
                var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
                if (player == null)
                {
                    return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
                }                // Points are awarded based on current stakes (what was at risk)
                var pointsAwarded = game.Stakes;
                var surrenderingTeam = (int)player.Team;
                var winningTeam = surrenderingTeam == (int)Team.PlayerTeam ? Team.OpponentTeam : Team.PlayerTeam;
                
                // Award points to the winning team
                game.TeamScores[winningTeam] += pointsAwarded;
                
                // Reset truco state
                game.TrucoCallState = TrucoCallState.None;
                game.LastTrucoCallerTeam = -1;
                game.CanRaiseTeam = null;

                // Publish event
                if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new TrucoSurrenderedEvent(
                        gameGuid,
                        player,
                        surrenderingTeam,
                        pointsAwarded,
                        game
                    ));
                }
                else
                {
                    _logger.LogWarning("Failed to parse GameId '{GameId}' for TrucoSurrenderedEvent", command.GameId);
                }

                return CommandResult.Success("Surrendered to Truco successfully");
            }
            catch (Exception ex)
            {
                return CommandResult.Failure($"Failed to surrender to Truco: {ex.Message}");
            }
        }        
        private async Task<CommandResult> ProcessSurrenderHandCommand(SurrenderHandCommand command, GameState game)
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
                game.GameStatus = "completed";

                // Determine winning team (opponent team wins)
                var winningTeam = player.Team == Team.PlayerTeam ? Team.OpponentTeam : Team.PlayerTeam;
                  if (Guid.TryParse(command.GameId, out var gameGuid))
                {
                    await _eventPublisher.PublishAsync(new PlayerFoldedEvent(
                        gameGuid,
                        player,
                        winningTeam.ToString(),
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
        }        private CommandResult ValidatePlayCardCommand(PlayCardCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            // Check if there's a pending truco call from the opposing team
            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player != null && game.TrucoCallState != TrucoCallState.None)
            {
                var playerTeam = (int)player.Team;
                if (game.LastTrucoCallerTeam != playerTeam)
                {
                    return CommandResult.Failure("Cannot play card while there's a pending Truco call to respond to");
                }
            }

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
        }        private CommandResult ValidateCallTrucoOrRaiseCommand(CallTrucoOrRaiseCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            if (game.IsBothTeamsAt10)
            {
                return CommandResult.Failure("Truco calls are disabled when both teams have 10 points");
            }

            if (game.TrucoCallState == TrucoCallState.Doze)
            {
                return CommandResult.Failure("Maximum truco level reached");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }

            // Check if this team can call/raise
            var playerTeam = (int)player.Team;
            if (game.LastTrucoCallerTeam == playerTeam)
            {
                return CommandResult.Failure("Cannot call/raise consecutively by the same team");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidateAcceptTrucoCommand(AcceptTrucoCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            if (game.TrucoCallState == TrucoCallState.None)
            {
                return CommandResult.Failure("No truco call to accept");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }

            // Check if player's team can respond
            var playerTeam = (int)player.Team;
            if (game.LastTrucoCallerTeam == playerTeam)
            {
                return CommandResult.Failure("Cannot accept your own team's truco call");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidateSurrenderTrucoCommand(SurrenderTrucoCommand command, GameState game)
        {
            if (game.GameStatus != "active")
            {
                return CommandResult.Failure("Game is not active");
            }

            if (game.TrucoCallState == TrucoCallState.None)
            {
                return CommandResult.Failure("No truco call to surrender to");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == command.PlayerSeat);
            if (player == null)
            {
                return CommandResult.Failure($"Player with seat {command.PlayerSeat} not found");
            }

            // Check if player's team can respond
            var playerTeam = (int)player.Team;
            if (game.LastTrucoCallerTeam == playerTeam)
            {
                return CommandResult.Failure("Cannot surrender to your own team's truco call");
            }

            return CommandResult.Success();
        }

        private CommandResult ValidateSurrenderHandCommand(SurrenderHandCommand command, GameState game)
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
        }        private List<string> GetAvailableActions(Player player, GameState game)
        {
            var actions = new List<string>();
            var playerTeam = (int)player.Team;

            // If game is not active, no actions available
            if (game.GameStatus != "active")
                return actions;

            // Truco-related actions based on game state
            if (game.TrucoCallState == TrucoCallState.None)
            {
                // No truco called yet - can call truco and play cards (if it's player's turn)
                actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                if (game.CurrentPlayerIndex == player.Seat)
                    actions.Add(TrucoConstants.PlayerActions.PlayCard);
            }
            else if (game.TrucoCallState == TrucoCallState.Truco || game.TrucoCallState == TrucoCallState.Seis)
            {
                // Truco/seis has been called - ONLY the opposing team can respond
                // No card play until truco is resolved
                if (game.LastTrucoCallerTeam != playerTeam)
                {
                    // Opposing team must respond
                    actions.Add(TrucoConstants.PlayerActions.AcceptTruco);
                    actions.Add(TrucoConstants.PlayerActions.SurrenderTruco);
                    
                    // Can raise if not at maximum stakes
                    if (game.Stakes < TrucoConstants.Stakes.Maximum)
                    {
                        actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                    }
                }
                // Team that called truco gets no actions - must wait for response
            }
            else
            {
                // Normal play - truco has been resolved or no truco state
                if (game.CurrentPlayerIndex == player.Seat)
                    actions.Add(TrucoConstants.PlayerActions.PlayCard);
                
                // Can call truco again if not at maximum stakes and can raise
                if (game.Stakes < TrucoConstants.Stakes.Maximum && 
                    (game.CanRaiseTeam == null || game.CanRaiseTeam == playerTeam))
                {
                    actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                }
            }

            // Always available: fold (surrender hand)
            actions.Add(TrucoConstants.PlayerActions.Fold);

            return actions;
        }
    }
}
