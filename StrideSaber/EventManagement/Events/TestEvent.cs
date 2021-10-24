using Serilog.Events;

namespace StrideSaber.EventManagement.Events
{
	public class TestEvent : Event
	{
		/// <inheritdoc />
		public override LogEventLevel? FiringLogLevel => LogEventLevel.Error;

		/// <inheritdoc />
		public override string? ToString()
		{
			return Id;
		}
	}
}