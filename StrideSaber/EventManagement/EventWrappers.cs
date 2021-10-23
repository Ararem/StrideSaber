using SmartFormat;
using System;

namespace StrideSaber.EventManagement
{
	partial class EventManager
	{
		/// <summary>
		/// The base class used to wrap events so they can be called by the <see cref="EventManager"/>
		/// </summary>
		private abstract class EventWrapper
		{
			/// <summary>
			/// Invokes the wrapped event
			/// </summary>
			public abstract void Invoke(Event e);
		}

		private sealed class Void_Param_EventWrapper<TEvent> : EventWrapper where TEvent : Event
		{
			public Void_Param_EventWrapper(Action<TEvent> action)
			{
				Action = action;
			}

			public Action<TEvent> Action { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				//This isn't safe to cast but that's okay, everything should be managed by the EventManager
				//and it's his fault if the wrong event type is passed in
				Action((TEvent)e);
			}
		}

		private sealed class Returns_Param_EventWrapper<TEvent> : EventWrapper where TEvent : Event
		{
			public Returns_Param_EventWrapper(Func<TEvent, object> action)
			{
				Action = action;
			}

			public Func<TEvent, object> Action { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				//This isn't safe to cast but that's okay, everything should be managed by the EventManager
				//and it's his fault if the wrong event type is passed in
				_ = Action((TEvent)e);
			}
		}
		private sealed class Void_NoParams_EventWrapper : EventWrapper
		{
			public Void_NoParams_EventWrapper(Action action)
			{
				Action = action;
			}

			public Action Action { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				Action();
			}
		}

		private sealed class Returns_NoParams_EventWrapper : EventWrapper
		{
			public Returns_NoParams_EventWrapper(Func<object> action)
			{
				Action = action;
			}

			public Func<object> Action { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				_ = Action();
			}
		}
	}
}