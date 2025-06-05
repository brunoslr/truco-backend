using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Models;
using TrucoMineiro.API.Constants;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for mapping between models and DTOs
    /// </summary>
    public class MappingService
    {        /// <summary>
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
        }/// <summary>
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
                PlayerId = player.Id,
                Name = player.Name,
                Team = player.Team,
                Hand = player.Hand.Select(card => MapCardToDto(card, false)).ToList(),
                IsDealer = player.IsDealer,
                IsActive = player.IsActive,
                Seat = player.Seat,
                FirstPlayerSeat = firstPlayerSeat
            };
        }

        /// <summary>
        /// Map a PlayedCard model to a PlayedCardDto
        /// </summary>
        public static PlayedCardDto MapPlayedCardToDto(PlayedCard playedCard)
        {
            return new PlayedCardDto
            {
                PlayerId = playedCard.PlayerId,
                Card = playedCard.Card != null ? MapCardToDto(playedCard.Card) : null
            };
        }

        /// <summary>
        /// Map an ActionLogEntry model to an ActionLogEntryDto
        /// </summary>
        public static ActionLogEntryDto MapActionLogEntryToDto(ActionLogEntry entry)
        {
            return new ActionLogEntryDto
            {
                Type = entry.Type,
                PlayerId = entry.PlayerId,
                Card = entry.Card,
                Action = entry.Action,
                HandNumber = entry.HandNumber,
                Winner = entry.Winner,
                WinnerTeam = entry.WinnerTeam
            };
        }

        /// <summary>
        /// Map a GameState model to a GameStateDto
        /// </summary>
        public static GameStateDto MapGameStateToDto(GameState gameState)
        {
            return new GameStateDto
            {
                Players = gameState.Players.Select(p => MapPlayerToDto(p, gameState.FirstPlayerSeat)).ToList(),
                PlayedCards = gameState.PlayedCards.Select(MapPlayedCardToDto).ToList(),
                Stakes = gameState.Stakes,
                IsTrucoCalled = gameState.IsTrucoCalled,
                IsRaiseEnabled = gameState.IsRaiseEnabled,
                CurrentHand = gameState.CurrentHand,
                TeamScores = gameState.TeamScores,
                TurnWinner = gameState.TurnWinner,
                ActionLog = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList()
            };
        }

        /// <summary>
        /// Map a GameState model to a GameStateDto with player-specific card visibility
        /// </summary>
        /// <param name="gameState">The game state to map</param>
        /// <param name="requestingPlayerSeat">The seat of the player requesting the game state (for card visibility)</param>
        /// <param name="showAllHands">Whether to reveal all player hands (DevMode)</param>
        public static GameStateDto MapGameStateToDto(GameState gameState, int requestingPlayerSeat, bool showAllHands = false)
        {
            return new GameStateDto
            {
                Players = gameState.Players.Select(p => MapPlayerToDto(p, gameState.FirstPlayerSeat, requestingPlayerSeat, showAllHands)).ToList(),
                PlayedCards = gameState.PlayedCards.Select(MapPlayedCardToDto).ToList(),
                Stakes = gameState.Stakes,
                IsTrucoCalled = gameState.IsTrucoCalled,
                IsRaiseEnabled = gameState.IsRaiseEnabled,
                CurrentHand = gameState.CurrentHand,
                TeamScores = gameState.TeamScores,
                TurnWinner = gameState.TurnWinner,
                ActionLog = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList()
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
                PlayerId = player.Id,
                Name = player.Name,
                Team = player.Team,
                Hand = player.Hand.Select(card => MapCardToDto(card, shouldHideCards)).ToList(),
                IsDealer = player.IsDealer,
                IsActive = player.IsActive,
                Seat = player.Seat,
                FirstPlayerSeat = firstPlayerSeat
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
                DealerSeat = gameState.DealerSeat,
                Stakes = gameState.Stakes,
                CurrentHand = gameState.CurrentHand,
                TeamScores = gameState.TeamScores,
                Actions = gameState.ActionLog.Select(MapActionLogEntryToDto).ToList()
            };            // Create teams
            response.Teams = new List<TeamDto>
            {
                new TeamDto 
                { 
                    Name = TrucoConstants.Teams.PlayerTeam, 
                    Seats = gameState.Players.Where(p => p.Team == TrucoConstants.Teams.PlayerTeam)
                                           .Select(p => p.Seat)
                                           .ToList() 
                },
                new TeamDto 
                { 
                    Name = TrucoConstants.Teams.OpponentTeam, 
                    Seats = gameState.Players.Where(p => p.Team == TrucoConstants.Teams.OpponentTeam)
                                           .Select(p => p.Seat)
                                           .ToList() 
                }
            };

            // Add players
            response.Players = gameState.Players.Select(p => new PlayerInfoDto
            {
                Name = p.Name,
                Seat = p.Seat,
                Team = p.Team
            }).ToList();            // Add the player's hand and all player hands
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
                {                    // For AI or other players, only show empty card objects
                    // This follows the requirement to just show the number of cards but not their values/suits
                    for (int i = 0; i < player.Hand.Count; i++)
                    {
                        playerHandDto.Cards.Add(new CardDto { Value = null, Suit = null });
                    }
                }
                
                response.PlayerHands.Add(playerHandDto);
            }            return response;
        }

        /// <summary>
        /// Map GameState to PlayCardResponseDto with proper card visibility
        /// </summary>
        public static PlayCardResponseDto MapGameStateToPlayCardResponse(GameState gameState, int playerSeat = 0, bool showAllHands = false, bool success = true, string message = "")
        {
            var response = new PlayCardResponseDto
            {
                Success = success,
                Message = message,
                GameState = MapGameStateToDto(gameState)
            };

            // Add the requesting player's hand
            var requestingPlayer = gameState.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (requestingPlayer != null)
            {
                response.Hand = requestingPlayer.Hand.Select(card => MapCardToDto(card, false)).ToList();
            }

            // Handle all player hands with proper visibility
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
                    // For AI or other players, only show hidden card objects
                    for (int i = 0; i < player.Hand.Count; i++)
                    {
                        playerHandDto.Cards.Add(new CardDto { Value = null, Suit = null });
                    }
                }
                
                response.PlayerHands.Add(playerHandDto);
            }

            return response;
        }
    }
}
