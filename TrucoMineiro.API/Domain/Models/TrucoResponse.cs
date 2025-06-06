namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Represents the possible responses to a Truco call
    /// </summary>
    public enum TrucoResponse
    {
        /// <summary>
        /// Accept the current stake
        /// </summary>
        Accept,

        /// <summary>
        /// Raise the stakes further
        /// </summary>
        Raise,

        /// <summary>
        /// Fold and give up the hand
        /// </summary>
        Fold
    }
}
