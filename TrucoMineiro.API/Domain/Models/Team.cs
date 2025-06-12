namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Team enumeration for type-safe team identification
    /// </summary>
    public enum Team
    {
        /// <summary>
        /// The player's team (seats 0 and 2)
        /// </summary>
        PlayerTeam = 1,

        /// <summary>
        /// The opponent team (seats 1 and 3)
        /// </summary>
        OpponentTeam = 2
    }
}
