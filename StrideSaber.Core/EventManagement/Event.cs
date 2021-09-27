using JetBrains.Annotations;
using Serilog.Events;
using StrideSaber.EventManagement.Events;

namespace StrideSaber.EventManagement
{
	/// <summary>
	///  An abstract class that encapsulates event information for the <see cref="EventManager"/>.
	/// </summary>
	/// <remarks>
	///  By the way, inherited classes should be treated as information-carriers, and shouldn't really actually do anything
	/// </remarks>
	[PublicAPI]
	[UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
	public abstract class Event
	{
		private protected Event()
		{
		}

		/// <summary>
		///  The string identifier for this event, for example "GameLoadEvent", "TestEvent", "Event64_420"
		/// </summary>
		public virtual string Id => GetType().Name;

		/// <summary>
		///  The level at which a message should be logged whenever this event is fired. If <see langword="null"/>, a message will not be logged
		/// </summary>
		/// <remarks>
		///  This should be <see langword="null"/> for high-frequency events like frame updates, but relatively high for important events (like a
		///  <see cref="GameLoadEvent">Game Load Event</see>)
		/// </remarks>
		public abstract LogEventLevel? FiringLogLevel { get; }

		/// <inheritdoc/>
		[Pure]
		public abstract override string ToString();
	}
}