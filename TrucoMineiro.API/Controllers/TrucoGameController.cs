using Microsoft.AspNetCore.Mvc;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Services;

namespace TrucoMineiro.API.Controllers
{
    /// <summary>
    /// Controller for managing Truco Mineiro games
    /// </summary>
    [Route("api/game")]
    [ApiController]
    [Produces("application/json")]
    public class TrucoGameController : ControllerBase
    {
        private readonly GameService _gameService;

        public TrucoGameController(GameService gameService)
        {
            _gameService = gameService;
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
        public ActionResult<GameStateDto> GetGameState(string gameId, [FromQuery] int? playerSeat = null)
        {
            var game = _gameService.GetGame(gameId);
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
            }

            // Check if DevMode is enabled to show all hands
            bool showAllHands = _gameService.IsDevMode();

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
        public ActionResult<GameStateDto> PressButton([FromBody] ButtonPressRequest request)
        {
            if (string.IsNullOrEmpty(request.GameId) || 
                request.PlayerSeat < 0 || request.PlayerSeat > 3 ||
                string.IsNullOrEmpty(request.Action))
            {
                return BadRequest("Invalid request parameters. PlayerSeat must be 0-3.");
            }

            bool success;
            string errorMessage;

            switch (request.Action.ToLower())
            {
                case ButtonPressActions.Truco:
                    success = _gameService.CallTruco(request.GameId, request.PlayerSeat);
                    errorMessage = "Invalid Truco call";
                    break;
                case ButtonPressActions.Raise:
                    success = _gameService.RaiseStakes(request.GameId, request.PlayerSeat);
                    errorMessage = "Invalid raise";
                    break;
                case ButtonPressActions.Fold:
                    success = _gameService.Fold(request.GameId, request.PlayerSeat);
                    errorMessage = "Invalid fold";
                    break;
                default:
                    return BadRequest($"Invalid action: {request.Action}. Valid actions are: {ButtonPressActions.Truco}, {ButtonPressActions.Raise}, {ButtonPressActions.Fold}");
            }

            if (!success)
            {
                return BadRequest(errorMessage);
            }

            var game = _gameService.GetGame(request.GameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            var gameStateDto = MappingService.MapGameStateToDto(game);
            return Ok(gameStateDto);
        }
        
        /// <summary>
        /// Starts a new hand in the current game
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/{gameId}/new-hand
        ///     
        /// This will deal new cards and start a new hand in the game.
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <response code="200">Returns the updated game state with the new hand</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpPost("{gameId}/new-hand")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateDto> StartNewHand(string gameId)
        {
            var success = _gameService.StartNewHand(gameId);
            if (!success)
            {
                return NotFound("Game not found");
            }

            var game = _gameService.GetGame(gameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            var gameStateDto = MappingService.MapGameStateToDto(game);
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
        public ActionResult<StartGameResponse> StartGame([FromBody] StartGameRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.PlayerName))
            {
                return BadRequest("Player name is required");
            }

            var game = _gameService.CreateGame(request.PlayerName);
            var response = MappingService.MapGameStateToStartGameResponse(game, 0, _gameService.IsDevMode());
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
        ///     
        /// This endpoint handles human players, AI players, and fold scenarios.
        /// In DevMode, AI players will automatically play their turns after a human player's move.
        /// Card visibility follows the same rules as the start game endpoint.
        /// </remarks>
        /// <param name="request">The play card request containing game ID, player seat, card index, and fold flag</param>
        /// <response code="200">Returns the updated game state with proper card visibility</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("play-card")]
        [ProducesResponseType(typeof(PlayCardResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<PlayCardResponseDto> PlayCardEnhanced([FromBody] PlayCardRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.GameId) || 
                request.PlayerSeat < 0 || request.PlayerSeat > 3 ||
                (!request.IsFold && request.CardIndex < 0))
            {
                var errorResponse = new PlayCardResponseDto
                {
                    Success = false,
                    Message = "Invalid request parameters. PlayerSeat must be 0-3.",
                    GameState = new GameStateDto(),
                    Hand = new List<CardDto>(),
                    PlayerHands = new List<PlayerHandDto>()
                };
                return BadRequest(errorResponse);
            }

            var response = _gameService.PlayCardEnhanced(
                request.GameId, 
                request.PlayerSeat, 
                request.CardIndex, 
                request.IsFold, 
                request.PlayerSeat
            );            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }
    }
}
