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
        /// Creates a new Truco Mineiro game
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game
        ///     
        /// This will create a new game with 4 players (2 teams) and deal the initial cards.
        /// </remarks>
        /// <response code="200">Returns the newly created game state</response>
        [HttpPost]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        public ActionResult<GameStateDto> CreateNewGame()
        {
            var game = _gameService.CreateGame();
            var gameStateDto = MappingService.MapGameStateToDto(game);
            return Ok(gameStateDto);
        }        /// <summary>
        /// Gets the current state of a specific game with player-specific card visibility
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/game/{gameId}?playerId=player1
        ///     
        /// This will return the current state of the specified game including cards,
        /// player information, scores, and game history. Card visibility is controlled
        /// based on the requesting player:
        /// - If playerId is provided, only that player's cards are visible (others are hidden)
        /// - If playerId is omitted, assumes single-player mode (human at seat 0)
        /// - In DevMode, all cards may be visible for debugging purposes
        /// - AI/other player cards are hidden by setting Value=null, Suit=null in production
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="playerId">Optional player ID for player-specific visibility. If omitted, assumes human player at seat 0</param>
        /// <response code="200">Returns the current game state with appropriate card visibility</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        /// <response code="400">If the specified player is not found in the game</response>
        [HttpGet("{gameId}")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<GameStateDto> GetGameState(string gameId, [FromQuery] string? playerId = null)
        {
            var game = _gameService.GetGame(gameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            // Determine the requesting player's seat
            int requestingPlayerSeat = 0; // Default to seat 0 (human player in single-player mode)
            
            if (!string.IsNullOrEmpty(playerId))
            {
                // If playerId is provided, find the player and validate they exist in the game
                var requestingPlayer = game.Players.FirstOrDefault(p => p.Id == playerId);
                if (requestingPlayer == null)
                {
                    return BadRequest($"Player '{playerId}' not found in game '{gameId}'");
                }
                requestingPlayerSeat = requestingPlayer.Seat;
            }

            // Check if DevMode is enabled to show all hands
            bool showAllHands = _gameService.IsDevMode();

            // Map the game state with player-specific visibility
            var gameStateDto = MappingService.MapGameStateToDto(game, requestingPlayerSeat, showAllHands);
            return Ok(gameStateDto);
        }/// <summary>
        /// Unified endpoint for button press actions (Truco, Raise, Fold)
        /// </summary>
        /// <remarks>
        /// Sample requests:
        /// 
        ///     POST /api/game/press-button
        ///     {
        ///         "gameId": "abc123",
        ///         "playerId": "player1",
        ///         "action": "truco"
        ///     }
        ///     
        ///     POST /api/game/press-button
        ///     {
        ///         "gameId": "abc123",
        ///         "playerId": "player2",
        ///         "action": "raise"
        ///     }
        ///     
        ///     POST /api/game/press-button
        ///     {
        ///         "gameId": "abc123",
        ///         "playerId": "player3",
        ///         "action": "fold"
        ///     }
        ///     
        /// This unified endpoint handles all button press actions:
        /// - "truco": Calls Truco to raise the stakes
        /// - "raise": Raises the stakes further after a Truco call
        /// - "fold": Folds the current hand, giving points to the opposing team
        /// </remarks>
        /// <param name="request">The button press request containing game ID, player ID, and action</param>
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
                string.IsNullOrEmpty(request.PlayerId) ||
                string.IsNullOrEmpty(request.Action))
            {
                return BadRequest("Invalid request parameters");
            }

            bool success;
            string errorMessage;

            switch (request.Action.ToLower())
            {
                case ButtonPressActions.Truco:
                    success = _gameService.CallTruco(request.GameId, request.PlayerId);
                    errorMessage = "Invalid Truco call";
                    break;
                case ButtonPressActions.Raise:
                    success = _gameService.RaiseStakes(request.GameId, request.PlayerId);
                    errorMessage = "Invalid raise";
                    break;
                case ButtonPressActions.Fold:
                    success = _gameService.Fold(request.GameId, request.PlayerId);
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
        }        /// <summary>
        /// Play a card from a player's hand (new endpoint with enhanced features)
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/play-card
        ///     {
        ///         "gameId": "abc123",
        ///         "playerId": "player1",
        ///         "cardIndex": 0,
        ///         "isFold": false
        ///     }
        ///     
        /// This endpoint handles human players, AI players, and fold scenarios.
        /// In DevMode, AI players will automatically play their turns after a human player's move.
        /// Card visibility follows the same rules as the start game endpoint.
        /// </remarks>
        /// <param name="request">The play card request containing game ID, player ID, card index, and fold flag</param>
        /// <response code="200">Returns the updated game state with proper card visibility</response>
        /// <response code="400">If the request is invalid</response>
        [HttpPost("play-card")]
        [ProducesResponseType(typeof(PlayCardResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public ActionResult<PlayCardResponseDto> PlayCardEnhanced([FromBody] PlayCardRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.GameId) || 
                string.IsNullOrWhiteSpace(request.PlayerId) ||
                (!request.IsFold && request.CardIndex < 0))
            {
                var errorResponse = new PlayCardResponseDto
                {
                    Success = false,
                    Message = "Invalid request parameters",
                    GameState = new GameStateDto(),
                    Hand = new List<CardDto>(),
                    PlayerHands = new List<PlayerHandDto>()
                };
                return BadRequest(errorResponse);
            }

            // Determine requesting player seat (assuming seat 0 for human player)
            var game = _gameService.GetGame(request.GameId);
            int requestingPlayerSeat = 0;
            if (game != null)
            {
                var requestingPlayer = game.Players.FirstOrDefault(p => p.Id == request.PlayerId);
                if (requestingPlayer != null)
                {
                    requestingPlayerSeat = requestingPlayer.Seat;
                }
            }

            var response = _gameService.PlayCardEnhanced(
                request.GameId, 
                request.PlayerId, 
                request.CardIndex, 
                request.IsFold, 
                requestingPlayerSeat
            );

            if (!response.Success)
            {
                return BadRequest(response);
            }

            return Ok(response);
        }    }

    /// <summary>
    /// Request DTO for enhanced play card endpoint
    /// </summary>
    public class PlayCardRequestDto
    {
        /// <summary>
        /// The unique identifier of the game
        /// </summary>
        /// <example>abc123</example>
        public string GameId { get; set; } = string.Empty;

        /// <summary>
        /// The unique identifier of the player making the move
        /// </summary>
        /// <example>player1</example>
        public string PlayerId { get; set; } = string.Empty;

        /// <summary>
        /// The index of the card in the player's hand to play (0-based)
        /// </summary>
        /// <example>0</example>
        public int CardIndex { get; set; }

        /// <summary>
        /// Flag indicating if the player is folding
        /// </summary>
        /// <example>false</example>
        public bool IsFold { get; set; }
    }

    /// <summary>
    /// Response DTO for play card actions
    /// </summary>
    public class PlayCardResponseDto
    {
        /// <summary>
        /// Indicates if the play card action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message providing additional information about the action result
        /// </summary>
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// The current state of the game after the action
        /// </summary>
        public GameStateDto GameState { get; set; } = new GameStateDto();

        /// <summary>
        /// The updated hand of the player who played the card
        /// </summary>
        public List<CardDto> Hand { get; set; } = new List<CardDto>();

        /// <summary>
        /// The updated hands of all players in the game
        /// </summary>
        public List<PlayerHandDto> PlayerHands { get; set; } = new List<PlayerHandDto>();
    }
}
