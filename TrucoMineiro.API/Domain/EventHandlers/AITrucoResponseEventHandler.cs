using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.Domain.StateMachine;
using TrucoMineiro.API.Domain.StateMachine.Commands;

namespace TrucoMineiro.API.Domain.EventHandlers
{
    /// <summary>
    /// Event handler for AI player responses to truco calls and raises
    /// </summary>
    public class AITrucoResponseEventHandler : IEventHandler<TrucoOrRaiseCalledEvent>
    {
        private readonly IAIPlayerService _aiPlayerService;
        private readonly IGameRepository _gameRepository;
        private readonly IGameStateMachine _gameStateMachine;
        private readonly ILogger<AITrucoResponseEventHandler> _logger;
        private readonly IConfiguration _configuration;

        public AITrucoResponseEventHandler(
            IAIPlayerService aiPlayerService,
            IGameRepository gameRepository,
            IGameStateMachine gameStateMachine,
            ILogger<AITrucoResponseEventHandler> logger,
            IConfiguration configuration)
        {
            _aiPlayerService = aiPlayerService;
            _gameRepository = gameRepository;
            _gameStateMachine = gameStateMachine;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Handle AI player responses to truco calls and raises
        /// </summary>
        public async Task HandleAsync(TrucoOrRaiseCalledEvent gameEvent, CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if AI auto-play is enabled
                var autoAiPlayEnabled = _configuration.GetValue<bool>("FeatureFlags:AutoAiPlay", true);
                if (!autoAiPlayEnabled)
                {
                    return;
                }

                var game = await _gameRepository.GetGameAsync(gameEvent.GameId.ToString());
                if (game == null)
                {
                    _logger.LogWarning("Game {GameId} not found for AI truco response", gameEvent.GameId);
                    return;
                }                // Find the AI player who needs to respond (from the opposing team)
                var callingPlayerTeam = gameEvent.CallerTeam;
                var respondingTeam = callingPlayerTeam == (int)Team.PlayerTeam ? Team.OpponentTeam : Team.PlayerTeam;

                // Get AI players from the responding team
                var aiPlayersToRespond = game.Players
                    .Where(p => p.IsAI && p.Team == respondingTeam)
                    .ToList();

                if (!aiPlayersToRespond.Any())
                {
                    _logger.LogDebug("No AI players need to respond to truco call in game {GameId}", gameEvent.GameId);
                    return;
                }

                // Select the first AI player to respond (in a real game, this would be more sophisticated)
                var respondingPlayer = aiPlayersToRespond.FirstOrDefault();
                if (respondingPlayer == null)
                {
                    return;
                }

                _logger.LogInformation("AI player {PlayerName} (seat {PlayerSeat}) responding to truco call in game {GameId}", 
                    respondingPlayer.Name, respondingPlayer.Seat, gameEvent.GameId);

                // Add thinking delay for realism
                await Task.Delay(GetAIThinkingDelay(), cancellationToken);

                // AI makes truco decision (simplified: always accept for now)
                var trucoDecision = _aiPlayerService.DecideTrucoResponse(respondingPlayer, game, gameEvent.CallType, gameEvent.NewPotentialStakes);

                // Execute the AI decision
                CommandResult result;
                switch (trucoDecision)
                {
                    case TrucoDecision.Accept:
                        var acceptCommand = new AcceptTrucoCommand(gameEvent.GameId.ToString(), respondingPlayer.Seat);
                        result = await _gameStateMachine.ProcessCommandAsync(acceptCommand);
                        _logger.LogInformation("AI player {PlayerName} accepted truco in game {GameId}", respondingPlayer.Name, gameEvent.GameId);
                        break;

                    case TrucoDecision.Surrender:
                        var surrenderCommand = new SurrenderTrucoCommand(gameEvent.GameId.ToString(), respondingPlayer.Seat);
                        result = await _gameStateMachine.ProcessCommandAsync(surrenderCommand);
                        _logger.LogInformation("AI player {PlayerName} surrendered to truco in game {GameId}", respondingPlayer.Name, gameEvent.GameId);
                        break;

                    case TrucoDecision.Raise:
                        var raiseCommand = new CallTrucoOrRaiseCommand(gameEvent.GameId.ToString(), respondingPlayer.Seat);
                        result = await _gameStateMachine.ProcessCommandAsync(raiseCommand);
                        _logger.LogInformation("AI player {PlayerName} raised truco in game {GameId}", respondingPlayer.Name, gameEvent.GameId);
                        break;

                    default:
                        _logger.LogWarning("AI player {PlayerName} made unknown truco decision: {Decision}", respondingPlayer.Name, trucoDecision);
                        return;
                }

                if (!result.IsSuccess)
                {
                    _logger.LogError("AI truco response failed for player {PlayerName} in game {GameId}: {Error}", 
                        respondingPlayer.Name, gameEvent.GameId, result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing AI truco response in game {GameId}", gameEvent.GameId);
            }
        }

        /// <summary>
        /// Generate realistic AI thinking delay using configuration values
        /// </summary>
        private TimeSpan GetAIThinkingDelay()
        {
            var minDelayMs = _configuration.GetValue<int>("GameSettings:AIMinPlayDelayMs", 500);
            var maxDelayMs = _configuration.GetValue<int>("GameSettings:AIMaxPlayDelayMs", 2000);
            
            if (minDelayMs <= 0 || maxDelayMs <= 0)
            {
                return TimeSpan.Zero;
            }
            
            if (minDelayMs > maxDelayMs)
            {
                (minDelayMs, maxDelayMs) = (maxDelayMs, minDelayMs);
            }
            
            var random = new Random();
            var delayMs = random.Next(minDelayMs, maxDelayMs + 1);
            
            return TimeSpan.FromMilliseconds(delayMs);
        }

        public bool CanHandle(TrucoOrRaiseCalledEvent gameEvent) => true;
    }
}
