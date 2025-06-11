using TrucoMineiro.API.Domain.Events;
using TrucoMineiro.API.Domain.Events.GameEvents;
using TrucoMineiro.API.Domain.Interfaces;
using TrucoMineiro.API.Domain.Models;
using TrucoMineiro.API.DTOs;
using TrucoMineiro.API.Services;

namespace TrucoMineiro.API.Domain.Services
{
    /// <summary>
    /// Service dedicated to handling all PlayCard-related logic and operations
    /// </summary>
    public class PlayCardService : IPlayCardService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IEventPublisher _eventPublisher;
        private readonly bool _devMode;

        public PlayCardService(
            IGameRepository gameRepository,
            IEventPublisher eventPublisher,
            IConfiguration configuration)
        {
            _gameRepository = gameRepository;
            _eventPublisher = eventPublisher;
            _devMode = configuration.GetValue<bool>("FeatureFlags:DevMode", false);
        }        /// <summary>
        /// Handles play card requests from the API controller
        /// </summary>
        /// <param name="request">The play card request</param>
        /// <returns>PlayCardResponseDto with the updated game state</returns>
        public async Task<PlayCardResponseDto> ProcessPlayCardRequestAsync(PlayCardRequestDto request)
        {
            // Validate basic request parameters
            if (string.IsNullOrWhiteSpace(request.GameId) || 
                request.PlayerSeat < 0 || request.PlayerSeat > 3 ||
                request.CardIndex < 0)
            {
                return CreateErrorResponse("Invalid request parameters. PlayerSeat must be 0-3 and CardIndex must be >= 0.");
            }

            // Get game state
            var game = await _gameRepository.GetGameAsync(request.GameId);
            if (game == null)
            {
                return CreateErrorResponse("Game not found");
            }

            // Validate card play
            var validationResult = ValidateCardPlay(game, request.PlayerSeat, request.CardIndex);
            if (!validationResult.IsValid)
            {
                return CreateErrorResponse(validationResult.ErrorMessage);
            }

            var player = game.Players.First(p => p.Seat == request.PlayerSeat);

            // Handle fold logic if requested
            if (request.IsFold)
            {
                PlayFoldCard(player, request.CardIndex);
            }

            // Execute card play (remove from hand and add to played cards)
            ExecuteCardPlay(game, player, request.CardIndex, request.IsFold);

            // Publish card played event
            await PublishCardPlayedEvent(game, player, request.IsFold);

            // Save game state
            await _gameRepository.SaveGameAsync(game);            return CreateSuccessResponse(game, request.PlayerSeat);
        }

        /// <summary>
        /// Validates if a card play is valid
        /// </summary>
        private (bool IsValid, string ErrorMessage) ValidateCardPlay(GameState game, int playerSeat, int cardIndex)
        {
            if (game.GameStatus != "active")
            {
                return (false, "Game is not active");
            }

            var player = game.Players.FirstOrDefault(p => p.Seat == playerSeat);
            if (player == null)
            {
                return (false, $"Player with seat {playerSeat} not found");
            }

            if (!player.IsActive)
            {
                return (false, "It's not this player's turn");
            }

            if (cardIndex < 0 || cardIndex >= player.Hand.Count)
            {
                return (false, "Invalid card index");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Replaces the card at the specified position with a fold card
        /// </summary>
        private void PlayFoldCard(Player player, int cardIndex)
        {
            // Replace the card with a fold card
            player.Hand[cardIndex] = Card.CreateFoldCard();
        }

        /// <summary>
        /// Executes the card play by removing from hand and adding to played cards
        /// </summary>
        private void ExecuteCardPlay(GameState game, Player player, int cardIndex, bool isFold)
        {
            // Get the card to play (either original card or fold card if fold was requested)
            var cardToPlay = player.Hand[cardIndex];
            
            // Remove from player's hand
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
        }        /// <summary>
        /// Publishes the card played event
        /// </summary>
        private async Task PublishCardPlayedEvent(GameState game, Player player, bool isFold)
        {
            var playedCard = game.PlayedCards.FirstOrDefault(pc => pc.PlayerSeat == player.Seat);
            if (playedCard?.Card != null)
            {
                await _eventPublisher.PublishAsync(new CardPlayedEvent(
                    Guid.Parse(game.GameId),
                    player.Id,
                    playedCard.Card,
                    player,
                    game.CurrentRound,
                    game.CurrentHand,
                    player.IsAI,
                    game
                ));
            }
        }

        /// <summary>
        /// Creates an error response with consistent format
        /// </summary>
        private PlayCardResponseDto CreateErrorResponse(string message)
        {
            return new PlayCardResponseDto
            {
                Success = false,
                Message = message,
                GameState = new GameStateDto(),
                Hand = new List<CardDto>(),
                PlayerHands = new List<PlayerHandDto>()
            };
        }

        /// <summary>
        /// Creates a success response with proper mapping
        /// </summary>
        private PlayCardResponseDto CreateSuccessResponse(GameState game, int requestingPlayerSeat)
        {
            return new PlayCardResponseDto
            {
                Success = true,
                Message = "Card played successfully",
                GameState = MappingService.MapGameStateToDto(game, requestingPlayerSeat, _devMode),
                Hand = game.Players.First(p => p.Seat == requestingPlayerSeat).Hand
                    .Select(card => MappingService.MapCardToDto(card, false)).ToList(),
                PlayerHands = game.Players.Select(p => new PlayerHandDto
                {
                    Seat = p.Seat,
                    Cards = p.Seat == requestingPlayerSeat || _devMode 
                        ? p.Hand.Select(card => MappingService.MapCardToDto(card, false)).ToList()
                        : p.Hand.Select(_ => new CardDto { Value = null, Suit = null }).ToList()
                }).ToList()
            };
        }
    }
}
