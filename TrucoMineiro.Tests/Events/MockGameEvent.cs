using TrucoMineiro.API.Domain.Events;

namespace TrucoMineiro.Tests.Events
{    /// <summary>
    /// Simple mock event for testing
    /// </summary>
    public class MockGameEvent : GameEventBase
    {
        public MockGameEvent(Guid gameId) : base(gameId)
        {
        }

        public override string EventType => "MockGameEvent";
    }
}
