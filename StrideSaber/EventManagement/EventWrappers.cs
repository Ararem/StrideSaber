using LibEternal.ObjectPools;
using System;
using System.Diagnostics;

// ReSharper disable InconsistentNaming

namespace StrideSaber.EventManagement
{
	partial class EventManager
	{
		/// <summary>
		///  The base class used to wrap events so they can be called by the <see cref="EventManager"/>
		/// </summary>
		private abstract class EventWrapper
		{
			public abstract Delegate Delegate { get; }

			/// <summary>
			///  Invokes the wrapped event
			/// </summary>
			public abstract void Invoke(Event e);

			/// <inheritdoc />
			public override string ToString()
			{
				return StringBuilderPool.BorrowInline(static (sb, wrapper) => sb.AppendTypeDisplayName(wrapper.GetType(), false, true), this);
			}
		}

		private sealed class Void_Param_EventWrapper<TEvent> : EventWrapper where TEvent : Event
		{
			public Void_Param_EventWrapper(Action<TEvent> @delegate)
			{
				Delegate = @delegate;
			}

			public override Action<TEvent> Delegate { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				//This isn't safe to cast but that's okay, everything should be managed by the EventManager
				//and it's his fault if the wrong event type is passed in
				Delegate((TEvent)e);
			}
		}

		private sealed class Returns_Param_EventWrapper<TEvent> : EventWrapper where TEvent : Event
		{
			public Returns_Param_EventWrapper(Func<TEvent, object> @delegate)
			{
				Delegate = @delegate;
			}

			public override Func<TEvent, object> Delegate { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				//This isn't safe to cast but that's okay, everything should be managed by the EventManager
				//and it's his fault if the wrong event type is passed in
				_ = Delegate((TEvent)e);
			}
		}

		private sealed class Void_NoParams_EventWrapper : EventWrapper
		{
			public Void_NoParams_EventWrapper(Action @delegate)
			{
				Delegate = @delegate;
			}

			public override Action Delegate { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				Delegate();
			}
		}

		private sealed class Returns_NoParams_EventWrapper : EventWrapper
		{
			public Returns_NoParams_EventWrapper(Func<object> @delegate)
			{
				Delegate = @delegate;
			}

			public override Func<object> Delegate { get; }

			/// <inheritdoc/>
			public override void Invoke(Event e)
			{
				_ = Delegate();
			}
		}
	}
}