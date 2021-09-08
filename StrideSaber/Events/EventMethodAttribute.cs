using System;

namespace StrideSaber.Events
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	public sealed class EventMethodAttribute : Attribute
	{
		// See the attribute guidelines at
		//  http://go.microsoft.com/fwlink/?LinkId=85236
		public EventMethodAttribute(Type type)
		{
			if (!type.IsAssignableTo(typeof(Event))) throw new ArgumentOutOfRangeException(nameof(type), type, "The given type must inherit from " + nameof(Event));
			EventType = type;
		}

		public readonly Type EventType;
	}
}