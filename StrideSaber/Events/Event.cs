using JetBrains.Annotations;

namespace StrideSaber.Events
{
	/// <summary>
	/// An abstract class that encapsulates event information for the <see cref="EventManager"/>.
	/// </summary>
	/// <remarks>
	/// The inherited versions (<see cref="ReusableEvent"/> and <see cref="NonReusableEvent"/>) should be used instead, to mark if the event can be reused or not. If the event has any instance data, it should be a <see cref="NonReusableEvent"/>, otherwise it should be a <see cref="ReusableEvent"/>.
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