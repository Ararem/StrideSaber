using JetBrains.Annotations;
using Serilog.Events;

namespace StrideSaber.Events
{
	/// <summary>
	/// An abstract class that encapsulates event information for the <see cref="EventManager"/>.
	/// </summary>
	/// <remarks>
	/// The inherited versions (<see cref="ReusableEvent"/> and <see cref="NonReusableEvent"/>) should be used instead, to mark if the event can be reused or not. If the event has any instance data, it should be a <see cref="NonReusableEvent"/>, otherwise it should be a <see cref="ReusableEvent"/>.
	/// By the way, inherited classes should be treated as information-carriers, and shouldn't really actually do anything
	/// </remarks>
	[PublicAPI, UsedImplicitly(ImplicitUseTargetFlags.WithInheritors)]
	public abstract class Event
	{
		private protected Event()
		{
		}

		/// <summary>
		/// The string identifier for this event, for example "GameLoadEvent", "TestEvent", "Event64_420"
		/// </summary>
		public virtual string Id => GetType().Name;

		/// <inheritdoc />
		[Pure]
		public abstract override string ToString();

		/// <summary>
		/// Whether this type of <see cref="Event"/> can be cached (reused). If true, this event type should not hold
		/// </summary>
		public abstract bool CanBeReused { get; }

		/// <summary>
		/// The level at which a message should be logged whenever this event is fired. If <see langword="null"/>, a message will not be logged
		/// </summary>
		/// <remarks>This should be <see langword="null"/> for high-frequency events like frame updates, but relatively high for important events (like a <see cref="GameLoadEvent"/>)</remarks>
		public abstract LogEventLevel? FiringLogLevel { get; }
	}

	/// <summary><inheritdoc cref="Event"/> This event type can be reused.</summary>
	public abstract class ReusableEvent : Event
	{
		/// <inheritdoc />
		public override bool CanBeReused => true;
	}

	/// <summary><inheritdoc cref="Event"/> This event type cannot be reused.</summary>
	public abstract class NonReusableEvent : Event
	{
		/// <inheritdoc />
		public override bool CanBeReused => false;
	}
}