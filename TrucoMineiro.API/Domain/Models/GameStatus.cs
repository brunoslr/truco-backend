namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Represents the current status of a Truco game
    /// </summary>
    public enum GameStatus
    {
        /// <summary>
        /// Game has been created but not yet started
        /// </summary>
        Waiting,
        
        /// <summary>
        /// Game is currently active and players are playing
        /// </summary>
        Active,
        
        /// <summary>
        /// Game has been completed and a winner determined
        /// </summary>
        Completed,
        
        /// <summary>
        /// Game has been paused or suspended
        /// </summary>
        Paused,
        
        /// <summary>
        /// Game was abandoned or cancelled
        /// </summary>
        Abandoned
    }
}
