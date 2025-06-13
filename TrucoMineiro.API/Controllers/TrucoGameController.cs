using Microsoft.AspNetCore.Mvc;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Services;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.StateMachine;
using TrucoMineiro.API.Domain.StateMachine.Commands;
using TrucoMineiro.API.Domain.Models;

namespace TrucoMineiro.API.Controllers
{    /// <summary>
     /// Controller for managing Truco Mineiro games
     /// </summary>
    [Route("api/game")]
    [ApiController]
    [Produces("application/json")]
    public class TrucoGameController : ControllerBase
    {
        private readonly IGameStateManager _gameStateManager;
        private readonly IGameRepository _gameRepository;
        private readonly IPlayCardService _playCardService;
        private readonly IGameStateMachine _gameStateMachine;
        private readonly IConfiguration _configuration;

        public TrucoGameController(
            IGameStateManager gameStateManager,
            IGameRepository gameRepository,
            IPlayCardService playCardService,
            IGameStateMachine gameStateMachine,
            IConfiguration configuration)
        {
            _gameStateManager = gameStateManager;
            _gameRepository = gameRepository;
            _playCardService = playCardService;
            _gameStateMachine = gameStateMachine;
            _configuration = configuration;
        }

        /// <summary>
        /// Health check endpoint to verify API availability
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/game/health
        ///     
        /// This endpoint returns a simple health status to verify the API is running.
        /// </remarks>
        /// <response code="200">Returns health status indicating the API is operational</response>
        [HttpGet("health")]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        public ActionResult<object> Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                service = "TrucoMineiro.API",
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Gets the current state of a specific game with player-specific card visibility
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/game/{gameId}?playerSeat=0
        ///     
        /// This will return the current state of the specified game including cards,
        /// player information, scores, and game history. Card visibility is controlled
        /// based on the requesting player:
        /// - If playerSeat is provided, only that player's cards are visible (others are hidden)
        /// - If playerSeat is omitted, assumes single-player mode (human at seat 0)
        /// - In DevMode, all cards may be visible for debugging purposes
        /// - AI/other player cards are hidden by setting Value=null, Suit=null in production
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerSeat">Optional player seat number (0-3) for player-specific visibility. If omitted, assumes human player at seat 0</param>
        /// <response code="200">Returns the current game state with appropriate card visibility</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        /// <response code="400">If the specified player seat is invalid</response>
        [HttpGet("{gameId}")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<GameStateDto>> GetGameState(string gameId, [FromQuery] int? playerSeat = null)
        {
            var game = await _gameRepository.GetGameAsync(gameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            // Determine the requesting player's seat
            int requestingPlayerSeat = playerSeat ?? 0; // Default to seat 0 (human player)

            if (requestingPlayerSeat < 0 || requestingPlayerSeat > 3)
            {
                return BadRequest("Player seat must be between 0 and 3");
            }

            // Validate that the seat exists in the game
            if (!game.Players.Any(p => p.Seat == requestingPlayerSeat))
            {
                return BadRequest($"Player at seat {requestingPlayerSeat} not found in game '{gameId}'");
            }            // Check if DevMode is enabled to show all hands
            bool showAllHands = _configuration.GetValue<bool>("FeatureFlags:DevMode", false);

            // Map the game state with player-specific visibility
            var gameStateDto = MappingService.MapGameStateToDto(game, requestingPlayerSeat, showAllHands);
            return Ok(gameStateDto);
        }

        /// <summary>
        /// Unified endpoint for button press actions (Truco, Raise, Fold)
        /// </summary>
        /// <remarks>
        /// Sample requests:
        /// 
        ///     POST /api/game/press-button
        ///     {
        ///         "gameId": "abc123",
        ///         "playerSeat": 0,
        ///         "action": "truco"
        ///     }
        ///     
        ///     POST /api/game/press-button
        ///     {
        ///         "gameId": "abc123",
        ///         "playerSeat": 1,
        ///         "action": "raise"
        ///     }
        ///     
        ///     POST /api/game/press-button
        ///     {
        ///         "gameId": "abc123",
        ///         "playerSeat": 2,
        ///         "action": "fold"
        ///     }
        ///     
        /// This unified endpoint handles all button press actions:
        /// - "truco": Calls Truco to raise the stakes
        /// - "raise": Raises the stakes further after a Truco call
        /// - "fold": Folds the current hand, giving points to the opposing team
        /// </remarks>
        /// <param name="request">The button press request containing game ID, player seat, and action</param>
        /// <response code="200">Returns the updated game state after the action</response>
        /// <response code="400">If the request is invalid or the action is not allowed</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpPost("press-button")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<GameStateDto>> PressButton([FromBody] ButtonPressRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId) ||
                request.PlayerSeat < 0 || request.PlayerSeat > 3 ||
                string.IsNullOrEmpty(request.Action))
            {
                return BadRequest("Invalid request parameters. PlayerSeat must be 0-3.");
            }            CommandResult result;            switch (request.Action)
            {
                case TrucoConstants.ButtonActions.CallTrucoOrRaise:
                case "calltrucooraise": // Legacy support
                case "truco": // Legacy support
                case "raise": // Legacy support
                    var trucoOrRaiseCommand = new CallTrucoOrRaiseCommand(request.GameId, request.PlayerSeat);
                    result = await _gameStateMachine.ProcessCommandAsync(trucoOrRaiseCommand);
                    break;
                    
                case TrucoConstants.ButtonActions.AcceptTruco:
                case "accepttruco": // Legacy support
                case "accept": // Legacy support
                    var acceptCommand = new AcceptTrucoCommand(request.GameId, request.PlayerSeat);
                    result = await _gameStateMachine.ProcessCommandAsync(acceptCommand);
                    break;
                    
                case TrucoConstants.ButtonActions.SurrenderTruco:
                case "surrendertruco": // Legacy support
                case "surrender": // Legacy support
                    var surrenderTrucoCommand = new SurrenderTrucoCommand(request.GameId, request.PlayerSeat);
                    result = await _gameStateMachine.ProcessCommandAsync(surrenderTrucoCommand);
                    break;
                    
                case TrucoConstants.ButtonActions.SurrenderHand:
                case "surrenderhand": // Legacy support
                case "fold": // Common alias
                    var foldCommand = new SurrenderHandCommand(request.GameId, request.PlayerSeat);
                    result = await _gameStateMachine.ProcessCommandAsync(foldCommand);
                    break;
                      default:
                    return BadRequest($"Invalid action: {request.Action}. Valid actions are: {TrucoConstants.ButtonActions.CallTrucoOrRaise}, {TrucoConstants.ButtonActions.AcceptTruco}, {TrucoConstants.ButtonActions.SurrenderTruco}, {TrucoConstants.ButtonActions.SurrenderHand}");
            }

            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);            }
            var game = await _gameRepository.GetGameAsync(request.GameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }
            
            // Check if DevMode is enabled to show all hands
            bool showAllHands = _configuration.GetValue<bool>("FeatureFlags:DevMode", false);
            
            var gameStateDto = MappingService.MapGameStateToDto(game, request.PlayerSeat, showAllHands);
            return Ok(gameStateDto);
        }

        /// <summary>
        /// Starts a new Truco Mineiro game with player name
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/start
        ///     {
        ///         "playerName": "John"
        ///     }
        ///     
        /// This will create a new game with the specified player name and return the initial game state.
        /// </remarks>
        /// <param name="request">The start game request containing player name</param>
        /// <response code="200">Returns the initial game state with player's cards</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("start")]
        [ProducesResponseType(typeof(StartGameResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<StartGameResponse>> StartGame([FromBody] StartGameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
            {
                return BadRequest("Player name is required");
            }            // Create the game using GameStateManager
            var game = await _gameStateManager.CreateGameAsync(request.PlayerName);
            if (game == null)
            {
                return BadRequest("Failed to create game");
            }
            // Then activate it using the StartGameCommand through the state machine
            var command = new StartGameCommand(game.GameId, request.PlayerName);
            var result = await _gameStateMachine.ProcessCommandAsync(command);
            if (!result.IsSuccess)
            {
                return BadRequest(result.ErrorMessage);
            }            // Get the updated game state
            var updatedGame = await _gameRepository.GetGameAsync(game.GameId);
            if (updatedGame == null)
            {
                return BadRequest("Failed to retrieve started game");
            }

            bool showAllHands = _configuration.GetValue<bool>("FeatureFlags:DevMode", false);
            var response = MappingService.MapGameStateToStartGameResponse(updatedGame, 0, showAllHands);
            return Ok(response);
        }
        /// <summary>
        /// Play a card from a player's hand (new endpoint with enhanced features)
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/play-card
        ///     {
        ///         "gameId": "abc123",
        ///         "playerSeat": 0,
        ///         "cardIndex": 0,
        ///         "isFold": false
        ///     }
        ///        /// This endpoint handles human players, AI players, and fold scenarios.
        /// When AutoAiPlay is enabled, AI players will automatically play their turns after a human player's move.
        /// Returns only status information - clients should poll GetGame endpoint for updated game state.
        /// </remarks>
        /// <param name="request">The play card request containing game ID, player seat, card index, and fold flag</param>
        /// <response code="200">Returns success/failure status with optional message or error details</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("play-card")]
        [ProducesResponseType(typeof(PlayCardResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<PlayCardResponseDto>> PlayCard([FromBody] PlayCardRequestDto request)
        {
            var response = await _playCardService.ProcessPlayCardRequestAsync(request);

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
