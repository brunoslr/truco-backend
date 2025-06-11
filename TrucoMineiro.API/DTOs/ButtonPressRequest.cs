namespace TrucoMineiro.API.DTOs
{
    /// <summary>
    /// Request DTO for unified button press actions (Truco, Raise, Fold)
    /// </summary>
    public class ButtonPressRequest
    {
        /// <summary>
        /// The unique identifier of the game
        /// </summary>
        /// <example>abc123</example>
        public string GameId { get; set; } = string.Empty;        /// <summary>
        /// The seat number of the player making the action (0-3)
        /// </summary>
        /// <example>0</example>
        public int PlayerSeat { get; set; }

        /// <summary>
        /// The type of button press action
        /// </summary>
        /// <example>truco</example>
        public string Action { get; set; } = string.Empty;
    }    /// <summary>
    /// Enumeration of valid button press actions
    /// </summary>
    public static class ButtonPressActions
    {
        public const string Truco = "truco";
        public const string Raise = "raise";
        public const string Surrender = "surrender";
    }
}
