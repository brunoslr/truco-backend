using TrucoMineiro.API.Constants;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.DTOs;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for mapping between models and DTOs
    /// </summary>
    public class MappingService
    {
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
        {            return new GameStateDto
            {
                Players = gameState.Players.Select(p => MapPlayerToDto(p, gameState.FirstPlayerSeat)).ToList(),
                PlayedCards = gameState.PlayedCards.Select(MapPlayedCardToDto).ToList(),
                Stakes = gameState.Stakes,
                TrucoCallState = gameState.TrucoCallState.ToString(),
                CurrentStakes = gameState.CurrentStakes,
                LastTrucoCallerTeam = gameState.LastTrucoCallerTeam,
                CanRaiseTeam = gameState.CanRaiseTeam,
                IsBothTeamsAt10 = gameState.IsBothTeamsAt10,
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
        public static GameStateDto MapGameStateToDto(GameState gameState, int requestingPlayerSeat, bool showAllHands = false)
        {
            var currentPlayer = gameState.Players.FirstOrDefault(p => p.Seat == requestingPlayerSeat);
            
            return new GameStateDto
            {
                Players = gameState.Players.Select(p => MapPlayerToDto(p, gameState.FirstPlayerSeat, requestingPlayerSeat, showAllHands)).ToList(),
                PlayedCards = gameState.PlayedCards.Select(MapPlayedCardToDto).ToList(),
                Stakes = gameState.Stakes,
                TrucoCallState = gameState.TrucoCallState.ToString(),
                CurrentStakes = gameState.CurrentStakes,
                LastTrucoCallerTeam = gameState.LastTrucoCallerTeam,
                CanRaiseTeam = gameState.CanRaiseTeam,
                IsBothTeamsAt10 = gameState.IsBothTeamsAt10,
                CurrentHand = gameState.CurrentHand,
                RoundWinners = gameState.RoundWinners.ToList(),
                TeamScores = gameState.TeamScores,
                IsGameComplete = gameState.IsCompleted,
                WinningTeam = gameState.IsCompleted ? gameState.WinningTeam : null,
                ActionLog = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList(),
                AvailableActions = currentPlayer != null ? GetAvailableActions(currentPlayer, gameState) : new List<string>()
            };
        }

        /// <summary>
        /// Map a Player model to a PlayerDto with player-specific card visibility
        /// </summary>
        /// <param name="player">The player to map</param>
        /// <param name="firstPlayerSeat">The seat of the first player</param>
        /// <param name="requestingPlayerSeat">The seat of the player requesting the game state</param>
        /// <param name="showAllHands">Whether to reveal all player hands (DevMode)</param>
        public static PlayerDto MapPlayerToDto(Player player, int firstPlayerSeat, int requestingPlayerSeat, bool showAllHands = false)
        {
            bool shouldHideCards = !showAllHands && player.Seat != requestingPlayerSeat;
            
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
        public static StartGameResponse MapGameStateToStartGameResponse(GameState gameState, int playerSeat = 0, bool showAllHands = false)
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

            return response;
        }

        /// <summary>
        /// Get available actions for a player based on current game state
        /// </summary>
        /// <param name="player">The player to get actions for</param>
        /// <param name="gameState">Current game state</param>
        /// <returns>List of available action strings</returns>
        private static List<string> GetAvailableActions(Player player, GameState gameState)
        {
            var actions = new List<string>();

            if (gameState.Status != GameStatus.Active)
            {
                return actions; // No actions available if game is not active
            }            // Check if there's a pending truco call (no card play allowed until resolved)
            var playerTeam = (int)player.Team;
            bool hasPendingTrucoCall = gameState.TrucoCallState != TrucoCallState.None;
            bool isRespondingTeam = gameState.LastTrucoCallerTeam != playerTeam;

            if (!hasPendingTrucoCall)
            {
                // Normal game actions
                actions.Add(TrucoConstants.PlayerActions.PlayCard);
                
                // Can call/raise truco if:
                // - Not at max level (Doze)
                // - Not in "Mão de 10" situation  
                // - This team didn't make the last call
                if (gameState.TrucoCallState != TrucoCallState.Doze && 
                    !gameState.IsBothTeamsAt10 && 
                    gameState.LastTrucoCallerTeam != playerTeam)
                {
                    actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                }
                
                actions.Add(TrucoConstants.PlayerActions.Fold);
            }            else
            {
                // Responding to a truco call - only the opposing team can respond
                if (isRespondingTeam)
                {
                    actions.Add(TrucoConstants.PlayerActions.AcceptTruco);
                    actions.Add(TrucoConstants.PlayerActions.SurrenderTruco);
                    
                    // Can raise if not at max level
                    if (gameState.TrucoCallState != TrucoCallState.Doze && !gameState.IsBothTeamsAt10)
                    {
                        actions.Add(TrucoConstants.PlayerActions.CallTrucoOrRaise);
                    }
                }
                // Team that called truco has no actions until the call is resolved
            }

            return actions;
        }
    }
}
