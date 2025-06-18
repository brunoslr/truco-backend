namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Represents AI decision options when responding to a truco call
    /// </summary>
    public enum TrucoDecision
    {
        /// <summary>
        /// Accept the truco call and continue playing with higher stakes
        /// </summary>
        Accept,

        /// <summary>
        /// Surrender to the truco call, awarding points to the calling team
        /// </summary>
        Surrender,

        /// <summary>
        /// Raise the stakes even higher (counter-raise)
        /// </summary>
        Raise
    }
}
