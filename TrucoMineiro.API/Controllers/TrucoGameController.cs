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
        /// Gets the current state of a specific game
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     GET /api/game/{gameId}
        ///     
        /// This will return the current state of the specified game including cards,
        /// player information, scores, and game history.
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <response code="200">Returns the current game state</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpGet("{gameId}")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateDto> GetGameState(string gameId)
        {
            var game = _gameService.GetGame(gameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            var gameStateDto = MappingService.MapGameStateToDto(game);
            return Ok(gameStateDto);
        }

        /// <summary>
        /// Plays a card from a player's hand
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/{gameId}/play-card
        ///     {
        ///         "playerId": "player1",
        ///         "cardIndex": 0
        ///     }
        ///     
        /// This will play the first card (index 0) from player1's hand.
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="request">The play card request containing player ID and card index</param>
        /// <response code="200">Returns the updated game state after playing the card</response>
        /// <response code="400">If the request is invalid or the move is not allowed</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpPost("{gameId}/play-card")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateDto> PlayCard(string gameId, [FromBody] PlayCardRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerId) || request.CardIndex < 0)
            {
                return BadRequest("Invalid request parameters");
            }

            var success = _gameService.PlayCard(gameId, request.PlayerId, request.CardIndex);
            if (!success)
            {
                return BadRequest("Invalid move");
            }

            var game = _gameService.GetGame(gameId);
            if (game == null)
            {
                return NotFound("Game not found");
            }

            var gameStateDto = MappingService.MapGameStateToDto(game);
            return Ok(gameStateDto);
        }        /// <summary>
        /// Calls Truco to raise the stakes
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/{gameId}/truco
        ///     {
        ///         "playerId": "player1"
        ///     }
        ///     
        /// This will call Truco, raising the stakes of the current hand.
        /// The opposing team will need to accept, raise further, or fold.
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="request">The Truco request containing player ID</param>
        /// <response code="200">Returns the updated game state after calling Truco</response>
        /// <response code="400">If the request is invalid or the Truco call is not allowed</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpPost("{gameId}/truco")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateDto> CallTruco(string gameId, [FromBody] TrucoRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerId))
            {
                return BadRequest("Invalid player ID");
            }

            var success = _gameService.CallTruco(gameId, request.PlayerId);
            if (!success)
            {
                return BadRequest("Invalid Truco call");
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
        /// Raises the stakes after a Truco call
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/{gameId}/raise
        ///     {
        ///         "playerId": "player1"
        ///     }
        ///     
        /// This will raise the stakes further after a Truco call.
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="request">The request containing player ID</param>
        /// <response code="200">Returns the updated game state after raising</response>
        /// <response code="400">If the request is invalid or raising is not allowed</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpPost("{gameId}/raise")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateDto> RaiseStakes(string gameId, [FromBody] TrucoRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerId))
            {
                return BadRequest("Invalid player ID");
            }

            var success = _gameService.RaiseStakes(gameId, request.PlayerId);
            if (!success)
            {
                return BadRequest("Invalid raise");
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
        /// Folds a hand in response to Truco or other challenge
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/game/{gameId}/fold
        ///     {
        ///         "playerId": "player1"
        ///     }
        ///     
        /// This will fold the current hand, giving points to the opposing team.
        /// </remarks>
        /// <param name="gameId">The unique identifier of the game</param>
        /// <param name="request">The request containing player ID</param>
        /// <response code="200">Returns the updated game state after folding</response>
        /// <response code="400">If the request is invalid or folding is not allowed</response>
        /// <response code="404">If the game with the specified ID doesn't exist</response>
        [HttpPost("{gameId}/fold")]
        [ProducesResponseType(typeof(GameStateDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<GameStateDto> Fold(string gameId, [FromBody] TrucoRequest request)
        {
            if (string.IsNullOrEmpty(request.PlayerId))
            {
                return BadRequest("Invalid player ID");
            }

            var success = _gameService.Fold(gameId, request.PlayerId);
            if (!success)
            {
                return BadRequest("Invalid fold");
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
        }    }    /// <summary>
    /// Request to play a card from a player's hand
    /// </summary>
    public class PlayCardRequest
    {
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
    }

    /// <summary>
    /// Request for actions like calling Truco, raising, or folding
    /// </summary>
    public class TrucoRequest
    {
        /// <summary>
        /// The unique identifier of the player making the action
        /// </summary>
        /// <example>player1</example>
        public string PlayerId { get; set; } = string.Empty;    }
}
