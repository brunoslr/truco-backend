namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Simplified response for the play card endpoint - status only approach
    /// For updated game state, clients should poll GetGame endpoint for consistent data source
    /// </summary>
    public class PlayCardResponseDto
    {
        /// <summary>
        /// Whether the action was successful
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Message describing the result of the action
        /// </summary>
        public string? Message { get; set; }

        /// <summary>
        /// Error details if the action failed
        /// </summary>
        public string? Error { get; set; }
    }
}
