using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Models;

namespace TrucoMineiro.API.Services
{
    /// <summary>
    /// Service for mapping between models and DTOs
    /// </summary>
    public class MappingService
    {
        /// <summary>
        /// Map a Card model to a CardDto
        /// </summary>
        public static CardDto MapCardToDto(Card card)
        {
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
                Value = dto.Value,
                Suit = dto.Suit
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
                Hand = player.Hand.Select(MapCardToDto).ToList(),
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
            };

            // Create teams
            response.Teams = new List<TeamDto>
            {
                new TeamDto 
                { 
                    Name = "Player's Team", 
                    Seats = gameState.Players.Where(p => p.Team == "Player's Team")
                                           .Select(p => p.Seat)
                                           .ToList() 
                },
                new TeamDto 
                { 
                    Name = "Opponent Team", 
                    Seats = gameState.Players.Where(p => p.Team == "Opponent Team")
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
            }).ToList();

            // Add the player's hand
            if (showAllHands)
            {
                // In DevMode, return all player hands
                var player = gameState.Players.FirstOrDefault(p => p.Seat == 0);
                if (player != null)
                {
                    response.Hand = player.Hand.Select(MapCardToDto).ToList();
                }
            }
            else
            {
                // Only show the requesting player's hand
                var player = gameState.Players.FirstOrDefault(p => p.Seat == playerSeat);
                if (player != null)
                {
                    response.Hand = player.Hand.Select(MapCardToDto).ToList();
                }
            }

            return response;
        }
    }
}
