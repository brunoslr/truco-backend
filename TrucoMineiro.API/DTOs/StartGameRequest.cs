namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Request to start a new Truco game
    /// </summary>
    public class StartGameRequest
    {
        /// <summary>
        /// The name of the player starting the game
        /// </summary>
        public string PlayerName { get; set; } = string.Empty;
    }
}
