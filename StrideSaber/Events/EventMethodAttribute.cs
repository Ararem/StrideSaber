using System;

namespace StrideSaber.Events
{
	/// <summary>
	/// An <see cref="Attribute"/> used to mark a method as an event method.
	/// This allows the method to be automatically called whenever a given <see cref="Event"/> is called. This attribute can be used multiple times per-method to subscribe it to multiple methods
	/// </summary>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public sealed class EventMethodAttribute : Attribute
	{
		// See the attribute guidelines at
		//  http://go.microsoft.com/fwlink/?LinkId=85236
		/// <summary>
		/// Constructs a new <see cref="EventMethodAttribute"/>
		/// </summary>
		/// <param name="type">A type argument for what type of <see cref="Event"/> the method should be subscribed to</param>
		public EventMethodAttribute(Type type)
		{
			if (!type.IsAssignableTo(typeof(Event))) throw new ArgumentOutOfRangeException(nameof(type), type, "The given type must inherit from " + nameof(Event));
			EventType = type;
		}

		/// <summary>
		/// The <see cref="Type"/> of <see cref="Event"/> the target method should be subscribed to
		/// </summary>
		public readonly Type EventType;
	}
}