using JetBrains.Annotations;
using System;

namespace StrideSaber.EventManagement
{
	/// <summary>
	///  An <see cref="Attribute"/> used to mark a method as an event method.
	///  This allows the method to be automatically called whenever a given <see cref="Event"/> is called. This attribute can be used multiple times
	///  per-method to subscribe it to multiple methods
	/// </summary>
	/// <remarks>
	///  The method is required to fulfill several requirements:
	///  <list type="number">
	///   <item>The method must be <see langword="static"/></item>
	///   <item>The method must have <b>a single</b> parameter of type <see cref="Event"/> (not an inherited or base class)</item>
	///   <item>The method must be marked with <see cref="EventMethodAttribute">this attribute</see></item>
	///  </list>
	/// </remarks>
	/// <example>
	///  <code>
	///    using Serilog;
	///    using StrideSaber.EventManagement;
	///    using StrideSaber.EventManagement.Events;
	///    
	///    namespace StrideSaber.Startup
	///    {
	///    	/// &lt;summary&gt;
	///    	/// Handles events that occur when the game is started
	///    	/// &lt;/summary&gt;
	///    	internal static class StartupEventHandler
	///    	{
	///    		[EventMethod(typeof(GameStartedEvent))]
	///    		private static void TestGameStartedEvent(Event @event)
	///    		{
	///    			Log.Information("Game started event called: {Event}", @event);
	///    		}
	///    		[EventMethod(typeof(GameLoadEvent))]
	///    		private static void TestGameLoadEvent(Event @event)
	///    		{
	///    			Log.Information("Game load event called: {Event}", @event);
	///    		}
	///    
	///    		//This method is subscribed to two different events - for when the game is loaded and when it is started
	///    		[EventMethod(typeof(GameStartedEvent))]
	///    		[EventMethod(typeof(GameLoadEvent))]
	///    		private static bool GameStartOrLoadEvent(Event @event)
	///    		{
	///    			if(@event is GameStartedEvent)
	///    				Log.Information("Game started event called: {Event}", @event);
	///   			else if(@event is GameLoadedEvent)
	///    				Log.Information("Game loaded event called: {Event}", @event);
	///    			else
	///    				Log.Warning("Something funky is happening"); //This should never actually happen
	/// 				return true;
	///    		}
	///    	}
	///    }
	///    </code>
	/// </example>
	[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
	[MeansImplicitUse(ImplicitUseTargetFlags.Itself)]
	public sealed class EventMethodAttribute : Attribute
	{
		/// <summary>
		///  The <see cref="Type"/> of <see cref="Event"/> the target method should be subscribed to
		/// </summary>
		public readonly Type EventType;

		/// <summary>
		///  Constructs a new <see cref="EventMethodAttribute"/>
		/// </summary>
		/// <param name="type">A type argument for what type of <see cref="Event"/> the method should be subscribed to</param>
		public EventMethodAttribute(Type type)
		{
			if (!type.IsAssignableTo(typeof(Event))) throw new ArgumentOutOfRangeException(nameof(type), type, "The given type must inherit from " + nameof(Event));
			EventType = type;
		}
	}
}