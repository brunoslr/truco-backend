using TrucoMineiro.API.DTOs;

namespace TrucoMineiro.API.Domain.Interfaces
{    /// <summary>
    /// Interface for handling all PlayCard-related operations
    /// </summary>
    public interface IPlayCardService
    {
        /// <summary>
        /// Handles play card requests from the API controller
        /// </summary>
        /// <param name="request">The play card request</param>
        /// <returns>PlayCardResponseDto with the updated game state</returns>
        Task<PlayCardResponseDto> ProcessPlayCardRequestAsync(PlayCardRequestDto request);
    }
}
