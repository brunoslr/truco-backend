namespace TrucoMineiro.API.Domain.Models
{
    /// <summary>
    /// Represents the current state of Truco calls and raises in a hand
    /// </summary>
    public enum TrucoCallState
    {
        /// <summary>
        /// No truco call in progress, stakes = 2
        /// </summary>
        None = 0,

        /// <summary>
        /// Truco called, waiting for response (stakes will be 4 if accepted)
        /// </summary>
        Truco = 1,

        /// <summary>
        /// Seis called, waiting for response (stakes will be 8 if accepted)
        /// </summary>
        Seis = 2,

        /// <summary>
        /// Doze called, waiting for response (stakes will be 12 if accepted)
        /// </summary>
        Doze = 3
    }
}
