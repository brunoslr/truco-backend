using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Domain.Interfaces;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for mapping between models and DTOs
    /// </summary>
    public class MappingService
    {
        private readonly ITrucoRulesEngine _trucoRulesEngine;

        public MappingService(ITrucoRulesEngine trucoRulesEngine)
        {
            _trucoRulesEngine = trucoRulesEngine;
        }
        /// <summary>
        /// Map a Card model to a CardDto with optional card hiding
        /// </summary>
        public static CardDto MapCardToDto(Card card, bool hideCard = false)
        {
            if (hideCard)
            {
                return new CardDto
                {
                    Value = null,
                    Suit = null
                };
            }
            
            return new CardDto
            {
                Value = card.Value,
                Suit = card.Suit
            };
        }

        /// <summary>
        /// Map a CardDto to a Card model
        /// </summary>
        public static Card MapDtoToCard(CardDto dto)
        {
            return new Card
            {
                Value = dto.Value ?? string.Empty,
                Suit = dto.Suit ?? string.Empty
            };
        }

        /// <summary>
        /// Map a Player model to a PlayerDto
        /// </summary>
        public static PlayerDto MapPlayerToDto(Player player, int firstPlayerSeat)
        {
            return new PlayerDto
            {
                Name = player.Name,
                Team = player.Team.ToString(),
                Hand = player.Hand.Select(card => MapCardToDto(card, false)).ToList(),
                IsDealer = player.IsDealer,
                IsActive = player.IsActive,
                Seat = player.Seat
            };
        }

        /// <summary>
        /// Map a PlayedCard model to a PlayedCardDto
        /// </summary>
        public static PlayedCardDto MapPlayedCardToDto(PlayedCard playedCard)
        {
            return new PlayedCardDto
            {
                PlayerSeat = playedCard.PlayerSeat,
                Card = MapCardToDto(playedCard.Card)
            };
        }

        /// <summary>
        /// Map an ActionLogEntry model to an ActionLogEntryDto
        /// Optimized to only include relevant fields based on action type
        /// </summary>
        public static ActionLogEntryDto MapActionLogEntryToDto(ActionLogEntry entry)
        {
            var dto = new ActionLogEntryDto
            {
                Type = entry.Type,
                Timestamp = entry.Timestamp,
                RoundNumber = entry.RoundNumber,
                PlayerSeat = entry.PlayerSeat
            };

            // Only include type-specific fields to reduce payload size
            switch (entry.Type)
            {
                case "card-played":
                    dto.Card = entry.Card;
                    break;
                    
                case "button-pressed":
                    dto.Action = entry.Action;
                    break;
                      case "hand-result":
                    dto.HandNumber = entry.HandNumber;
                    dto.Winner = entry.Winner?.ToString();
                    dto.WinnerTeam = entry.WinnerTeam?.ToString();
                    break;
                    
                case "turn-result":
                    dto.Winner = entry.Winner?.ToString();
                    dto.WinnerTeam = entry.WinnerTeam?.ToString();
                    break;
                    
                case "turn-start":
                case "game-started":
                default:
                    // Only Type, Timestamp, RoundNumber and PlayerSeat are needed for these action types
                    break;
            }

            return dto;
        }        /// <summary>
        /// Map a GameState model to a GameStateDto
        /// </summary>
        public static GameStateDto MapGameStateToDto(GameState gameState)
        {            // TODO: LEGACY CLEANUP - This static method should be removed in favor of the instance method
            // For now, create a temporary TrucoRulesEngine instance for dynamic calculation
            var tempRulesEngine = new Domain.Services.TrucoRulesEngine();
            
            return new GameStateDto
            {
                Players = gameState.Players.Select(p => MapPlayerToDto(p, gameState.FirstPlayerSeat)).ToList(),
                PlayedCards = gameState.PlayedCards.Select(MapPlayedCardToDto).ToList(),                Stakes = gameState.Stakes,
                TrucoCallState = gameState.TrucoCallState.ToString(),
                CurrentStakes = gameState.CurrentStakes,
                LastTrucoCallerTeam = gameState.LastTrucoCallerTeam,
                CanRaiseTeam = gameState.CanRaiseTeam,
                IronHandEnabled = gameState.IronHandEnabled,
                PartnerCardVisibilityEnabled = gameState.PartnerCardVisibilityEnabled,
                CurrentHand = gameState.CurrentHand,
                RoundWinners = gameState.RoundWinners.ToList(),
                TeamScores = gameState.TeamScores,
                IsGameComplete = gameState.IsCompleted,
                WinningTeam = gameState.IsCompleted ? gameState.WinningTeam : null,
                ActionLog = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList()
            };
        }        /// <summary>
        /// Map a GameState model to a GameStateDto with player-specific card visibility
        /// </summary>
        /// <param name="gameState">The game state to map</param>        /// <param name="requestingPlayerSeat">The seat of the player requesting the game state (for card visibility)</param>
        /// <param name="showAllHands">Whether to reveal all player hands (DevMode)</param>
        public GameStateDto MapGameStateToDto(GameState gameState, int requestingPlayerSeat, bool showAllHands = false)
        {
            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Seat == requestingPlayerSeat);
              return new GameStateDto
            {
                Players = gameState.Players.Select(p => MapPlayerToDto(p, gameState, requestingPlayerSeat, showAllHands)).ToList(),
                PlayedCards = gameState.PlayedCards.Select(MapPlayedCardToDto).ToList(),                Stakes = gameState.Stakes,
                TrucoCallState = gameState.TrucoCallState.ToString(),
                CurrentStakes = gameState.CurrentStakes,
                LastTrucoCallerTeam = gameState.LastTrucoCallerTeam,
                CanRaiseTeam = gameState.CanRaiseTeam,
                IronHandEnabled = gameState.IronHandEnabled,
                PartnerCardVisibilityEnabled = gameState.PartnerCardVisibilityEnabled,
                CurrentHand = gameState.CurrentHand,
                RoundWinners = gameState.RoundWinners.ToList(),
                TeamScores = gameState.TeamScores,
                IsGameComplete = gameState.IsCompleted,
                WinningTeam = gameState.IsCompleted ? gameState.WinningTeam : null,
                ActionLog = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList(),
                AvailableActions = currentPlayer != null ? GetAvailableActions(currentPlayer, gameState) : new List<string>()
            };
        }        /// <summary>
        /// Map a Player model to a PlayerDto with advanced card visibility rules
        /// </summary>
        /// <param name="player">The player to map</param>
        /// <param name="gameState">The current game state (for special rule context)</param>
        /// <param name="requestingPlayerSeat">The seat of the player requesting the game state</param>
        /// <param name="showAllHands">Whether to reveal all player hands (DevMode)</param>
        public PlayerDto MapPlayerToDto(Player player, GameState gameState, int requestingPlayerSeat, bool showAllHands = false)
        {
            // Determine card visibility based on game rules
            bool shouldHideCards = ShouldHidePlayerCards(player, gameState, requestingPlayerSeat, showAllHands);
            
            return new PlayerDto
            {
                Name = player.Name,
                Team = player.Team.ToString(),
                Hand = player.Hand.Select(card => MapCardToDto(card, shouldHideCards)).ToList(),
                IsDealer = player.IsDealer,
                IsActive = player.IsActive,
                Seat = player.Seat
            };
        }

        /// <summary>
        /// Map a GameState model to a StartGameResponse
        /// </summary>
        /// <param name="gameState">The game state to map</param>
        /// <param name="playerSeat">The seat of the requesting player</param>
        /// <param name="showAllHands">Whether to reveal all player hands (DevMode)</param>
        public StartGameResponse MapGameStateToStartGameResponse(GameState gameState, int playerSeat = 0, bool showAllHands = false)
        {
            var response = new StartGameResponse
            {
                GameId = gameState.GameId,
                PlayerSeat = playerSeat,
                DealerSeat = gameState.DealerSeat,                Stakes = gameState.Stakes,
                CurrentHand = gameState.CurrentHand,
                TeamScores = gameState.TeamScores.ToDictionary(kvp => kvp.Key.ToString(), kvp => kvp.Value),
                Actions = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList()
            };            
            
            // Create teams
            response.Teams = new List<TeamDto>
            {
                new TeamDto 
                { 
                    Name = Team.PlayerTeam.ToString(), 
                    Seats = gameState.Players.Where(p => p.Team == Team.PlayerTeam)
                                           .Select(p => p.Seat)
                                           .ToList() 
                },
                new TeamDto 
                { 
                    Name = Team.OpponentTeam.ToString(), 
                    Seats = gameState.Players.Where(p => p.Team == Team.OpponentTeam)
                                           .Select(p => p.Seat)
                                           .ToList() 
                }
            };            // Add players
            response.Players = gameState.Players.Select(p => new PlayerInfoDto
            {
                Name = p.Name,
                Seat = p.Seat,
                Team = p.Team.ToString()
            }).ToList();

            // Add the player's hand and all player hands
            var requestingPlayer = gameState.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (requestingPlayer != null)
            {
                // Set the requesting player's hand in the Hand property
                response.Hand = requestingPlayer.Hand.Select(card => MapCardToDto(card, false)).ToList();
            }
            
            // Handle all player hands
            response.PlayerHands = new List<PlayerHandDto>();
            foreach (var player in gameState.Players)
            {
                var playerHandDto = new PlayerHandDto
                {
                    Seat = player.Seat,
                    Cards = new List<CardDto>()
                };
                
                if (showAllHands || player.Seat == playerSeat)
                {
                    // In DevMode or for the requesting player, show actual cards
                    playerHandDto.Cards = player.Hand.Select(card => MapCardToDto(card, false)).ToList();
                }
                else
                {
                    // For AI or other players, only show empty card objects
                    // This follows the requirement to just show the number of cards but not their values/suits
                    for (int i = 0; i < player.Hand.Count; i++)
                    {
                        playerHandDto.Cards.Add(new CardDto { Value = null, Suit = null });
                    }
                }
                
                response.PlayerHands.Add(playerHandDto);
            }

            return response;        }

        /// <summary>
        /// Get available actions for a player based on current game state
        /// </summary>
        /// <param name="player">The player to get actions for</param>
        /// <param name="gameState">Current game state</param>
        /// <returns>List of available action strings</returns>
        private List<string> GetAvailableActions(Player player, GameState gameState)
        {
            var actions = new List<string>();

            if (gameState.Status != GameStatus.Active)
            {
                return actions; // No actions available if game is not active
            }

            // Check if there's a pending truco call (no card play allowed until resolved)
            var playerTeam = (int)player.Team;
            bool hasPendingTrucoCall = gameState.TrucoCallState != TrucoCallState.None;
            bool isRespondingTeam = gameState.LastTrucoCallerTeam != playerTeam;            // Use TrucoRulesEngine for dynamic last hand detection
            bool isLastHandActive = _trucoRulesEngine.IsLastHand(gameState);
            bool isOneTeamAtLastHand = _trucoRulesEngine.IsOneTeamAtLastHand(gameState);
            bool areBothTeamsAtLastHand = _trucoRulesEngine.AreBothTeamsAtLastHand(gameState);
            bool isMaximumStakes = _trucoRulesEngine.IsMaximumStakes(gameState);

            if (!hasPendingTrucoCall)
            {
                // Normal game actions
                actions.Add(TrucoConstants.PlayerActions.PlayCard);                // Can call/raise truco if:
                // - Not at max level (stakes not at maximum and not pending max call)
                // - Not in last hand situation where truco is disabled
                // - This team didn't make the last call
                if (!isMaximumStakes && // Not already at max stakes
                    !areBothTeamsAtLastHand && // Truco completely disabled when both teams at last hand
                    gameState.LastTrucoCallerTeam != playerTeam)
                {                    // Special rule: if only one team is at last hand, only that team can call truco
                    if (!isOneTeamAtLastHand || (isOneTeamAtLastHand && (int)_trucoRulesEngine.GetTeamAtLastHand(gameState)! == playerTeam))
                    {
                        actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                    }
                }
                
                actions.Add(TrucoConstants.PlayerActions.Fold);
            }
            else
            {
                // Responding to a truco call - only the opposing team can respond
                if (isRespondingTeam)
                {
                    actions.Add(TrucoConstants.PlayerActions.AcceptTruco);
                    actions.Add(TrucoConstants.PlayerActions.SurrenderTruco);                    // Can raise if not at max level and last hand rules allow it
                    if (!isMaximumStakes && // Not already at max stakes
                        !areBothTeamsAtLastHand) // Truco raising disabled when both teams at last hand
                    {                        // Special rule: if only one team is at last hand, only that team can raise
                        if (!isOneTeamAtLastHand || (isOneTeamAtLastHand && (int)_trucoRulesEngine.GetTeamAtLastHand(gameState)! == playerTeam))
                        {
                            actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                        }
                    }
                }
                // Team that called truco has no actions until the call is resolved
            }

            return actions;
        }

        /// <summary>
        /// Determines whether to hide a player's cards based on game rules and special features
        /// </summary>
        /// <param name="player">The player whose cards are being shown</param>
        /// <param name="gameState">The current game state</param>
        /// <param name="requestingPlayerSeat">The seat of the player requesting the view</param>
        /// <param name="showAllHands">DevMode flag to show all hands</param>
        /// <returns>True if cards should be hidden, false if they should be visible</returns>
        private bool ShouldHidePlayerCards(Player player, GameState gameState, int requestingPlayerSeat, bool showAllHands)
        {
            // DevMode: show all cards
            if (showAllHands)
                return false;

            var requestingPlayer = gameState.Players.FirstOrDefault(p => p.Seat == requestingPlayerSeat);
            if (requestingPlayer == null)
                return true; // Hide if requesting player not found

            // Iron Hand Rule: during last hand, players cannot see their own cards
            bool isLastHand = _trucoRulesEngine.IsLastHand(gameState);
            if (isLastHand && gameState.IronHandEnabled && player.Seat == requestingPlayerSeat)
                return true;

            // Partner Card Visibility: during last hand, show partner's cards if feature is enabled
            if (isLastHand && gameState.PartnerCardVisibilityEnabled)
            {
                var requestingPlayerTeam = requestingPlayer.Team;
                var playerTeam = player.Team;

                // Show own cards (unless Iron Hand is active)
                if (player.Seat == requestingPlayerSeat)
                    return gameState.IronHandEnabled; // Hidden only if Iron Hand is active

                // Show partner's cards if on same team
                if (playerTeam == requestingPlayerTeam)
                    return false; // Show partner's cards
            }

            // Standard rule: show only own cards
            return player.Seat != requestingPlayerSeat;
        }
    }
}
