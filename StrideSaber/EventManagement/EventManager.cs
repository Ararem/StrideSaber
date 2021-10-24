using ConcurrentCollections;
using Serilog;
using Serilog.Context;
using Stride.Core.Extensions;
using StrideSaber.EventManagement.Events;
using StrideSaber.Modding;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StrideSaber.EventManagement
{
	/// <summary>
	///  A manager class that manages all <see cref="Event">events</see> for the program
	/// </summary>
	public static partial class EventManager
	{
		/// <summary>
		///  The map of methods to their events. Use a <see cref="Type"/> (that inherits from <see cref="Event"/>) as the key to access all methods subscribed to
		///  that type of event.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, ConcurrentHashSet<EventWrapper>> EventMethods = new();

		internal static void Init()
		{
			var sw = Stopwatch.StartNew();
			Log.Information("Initializing event manager");

			IndexEventMethods();

			sw.Stop();
			Log.Information("Event manager initialized in {Elapsed:g}", sw.Elapsed);
		}

		private static void IndexEventMethods()
		{
			Log.Debug("Indexing event methods");

			MethodIndexingStats stats = new();
			Stopwatch sw = Stopwatch.StartNew();

			//Clear the event methods. To avoid wasting memory, just reuse the old hashsets we already have, but remove any items from them
			//(Instead of removing all the sets from the dictionary and wasting all that precious memory)
			EventMethods.Values.ForEach(set => set.Clear());

			//Loop over all the assemblies we have
			foreach (Assembly assembly in AssemblyManager.GetAllExternalAssemblies())
				IndexAssembly(assembly, ref stats);
			//Everything done, stop the clock
			sw.Stop();
			Log.Debug("Indexed event methods in {Elapsed:g}", sw.Elapsed);
			Log.Debug("Indexing Statistics: {@IndexingStats}", stats);
		}

		private static void IndexAssembly(Assembly assembly, ref MethodIndexingStats stats)
		{
			stats.AssemblyScanCount++;
			//For debugging purposes
			Log.Debug("Scanning assembly {Assembly}", assembly);
			using IDisposable _ = LogContext.PushProperty("Assembly", assembly);

			Type[] types;
			//Yeah so sometimes this fails
			try
			{
				types = assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				types = ex.Types.Where(t => t is not null).ToArray()!;
				Log.Warning(ex, "Could not load all types from assembly {Assembly}", assembly);
			}

			Log.Debug("Got {Count} types from assembly {Assembly}", types.Length, assembly);
			foreach (Type type in types) IndexType(type, ref stats);
		}

		private static void IndexType(Type type, ref MethodIndexingStats stats)
		{
			stats.TypeScanCount++;
			Log.Verbose("Scanning type {Type}", type);
			using IDisposable _ = LogContext.PushProperty("Type", type);

			//Grab all the methods, well sort out the invalid ones later
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo method in methods) IndexRawMethod(method, ref stats);
		}

		private static void IndexRawMethod(MethodInfo method, ref MethodIndexingStats stats)
		{
			stats.MethodScanCount++;

			EventMethodAttribute[] eventAttributes = method.GetCustomAttributes<EventMethodAttribute>().ToArray();
			if (eventAttributes.Length == 0) return; //Skip the method if it's not marked

			if (!method.IsStatic)
			{
				Log.Warning("Method {$Method} is non-static (instance)", method);
				stats.InvalidMethodCount++;
				return;
			}

			ParameterInfo[] parameters = method.GetParameters();
			if (parameters.Length > 1) //We only allow 0 or 1 parameters
			{
				Log.Warning("Method {$Method} has invalid parameter count of {Count}. Must be <= 1", method, parameters.Length);
				stats.InvalidMethodCount++;
				return;
			}

			//If it takes in a parameter
			if (parameters.Length == 1)
			{
				ParameterInfo parameter = parameters[0];
				//Ensure that it inherits from event
				if (!parameter.ParameterType.IsAssignableTo(typeof(Event)))
				{
					Log.Warning("Method {$Method} has incorrect parameter type {Type} (must inherit from {EventBaseType})", method, parameter.ParameterType, typeof(Event));
					stats.InvalidMethodCount++;
					return;
				}
			}

			Type[] eventTypes = eventAttributes.Select(static a => a.EventType).ToArray();
			bool anySuccessful = false;
			foreach (Type eventType in eventTypes)
			{
				//Was this target successful?
				bool success = IndexMethodForEventType(method, eventType);
				if (success)
					stats.DuplicateMethodCount++;
				else
					stats.InvalidMethodCount++;
				//OR it so that if any of the targets succeeded, the 'any' flag is true
				anySuccessful |= success;
			}

			//Mark it *once* if any targets were valid
			if (anySuccessful)
				stats.ActualMethodCount++;
		}

		//We don't have to pass in the 'ref stats', because we return true/false to indicate success or failure
		private static bool IndexMethodForEventType(MethodInfo method, Type eventType)
		{
			//Here, we ensure that the dictionary has a set for us to store actions in.
			//If the event type already has a set to store in, that's what is returned, otherwise we make a new one.
			//I'm passing in a delegate instead of instantiating a new set so that we only allocate memory if we actually need it
			var set = EventMethods.GetOrAdd(eventType, static _ => new ConcurrentHashSet<EventWrapper>());
			EventWrapper wrapper;
			//No params and void, easy peasy
			if ((method.ReturnType == typeof(void)) && (method.GetParameters().Length == 0))
			{
				wrapper = new Void_NoParams_EventWrapper(method.CreateDelegate<Action>());
			}
			//No params but returns, also easy
			else if ((method.GetParameters().Length == 0) && (method.ReturnType != typeof(void)))
			{
				wrapper = new Returns_NoParams_EventWrapper(method.CreateDelegate<Func<object>>());
			}
			//Here's where it gets tricky...
			else
			{
				//Check here what type of parameter it requires
				//This is also the type of the argument we need to pass in as a generic type arg
				Type paramType = method.GetParameters()[0].ParameterType;
				//Here we need to validate that the parameter matches the type for the event
				//Need to ensure that the EventType can be casted into the ParamType
				if (!eventType.IsAssignableTo(paramType))
				{
					Log.Warning("Cannot cast from attribute event type {EventType} to parameter type {ParamType} for method {Method}", eventType, paramType, method);
					return false;
				}

				if (method.ReturnType == typeof(void))
				{
					//It's a void returning method, wrap it into a Void_Param_EventWrapper<TEvent>
					Type wrapperType = typeof(Void_Param_EventWrapper<>)
							.MakeGenericType(paramType);                                               //Have to pass in the generic type arg
					Delegate del = method.CreateDelegate(typeof(Action<>).MakeGenericType(paramType)); //Generalise the action type for our parameter
					wrapper = (EventWrapper)Activator.CreateInstance(wrapperType, del)!;               //The constructor should have an appropriate input type (I hope)
				}
				else
				{
					//It's an object returning method, wrap it into a Returns_Param_EventWrapper<TEvent>
					Type wrapperType = typeof(Returns_Param_EventWrapper<>)
							.MakeGenericType(paramType);                                                              //Have to pass in the generic type arg
					Delegate del = method.CreateDelegate(typeof(Func<,>).MakeGenericType(paramType, typeof(object))); //Generalise the func delegate type. We can safely cast the return 'down' to object, but we mustn't do so for the event type
					wrapper = (EventWrapper)Activator.CreateInstance(wrapperType, del)!;                              //The constructor should have an appropriate input type (I hope)
				}
			}

			set.Add(wrapper);
			Log.Verbose("Added method {Method} for event type {Type} (Wrapper={$Wrapper})", method, eventType, wrapper);
			return true;
		}

	#region Event storage and invocation

		/// <summary>
		///  Fires (invokes) an <see cref="Event"/> of type <typeparamref name="T"/>, catching and logging any <see cref="Exception">Exceptions</see>
		/// </summary>
		/// <param name="e">The <see cref="Event"/> to fire</param>
		public static void FireEventSafeLogged<T>(T e) where T : Event
		{
			var exceptions = FireEventSafe(e);
			if (exceptions.Count == 0) return;
			foreach (Exception exception in exceptions) Log.Verbose(exception, "[{Event}]: Caught exception", e);
		}

		/// <summary>
		///  Fires (invokes) an <see cref="Event"/> of type <typeparamref name="T"/>, catching and returning any <see cref="Exception">Exceptions</see>
		/// </summary>
		/// <param name="evt">The <see cref="Event"/> to fire</param>
		public static List<Exception> FireEventSafe<T>(T evt) where T : Event
		{
			EventMethods.TryGetValue(typeof(T), out var events);
			//Here we catch any exceptions the code might throw
			List<Exception> exceptions = new(1);
			bool log = evt.FiringLogLevel is not null;
			//TODO: Yeah how thread-safe is this?
			if (log) Log.Write(evt.FiringLogLevel!.Value, "Firing event {EventId} for {Count} subscribers: {Event}", evt.Id, events?.Count ?? 0, evt);
			if (events != null)
				foreach (EventWrapper wrapper in events)
				{
					if (log) //I only want this to be logged in very rare circumstances
						Log.Verbose("[{EventId}]: Invoking method {@Delegate} (Wrapper={$Wrapper})", evt.Id, wrapper.Delegate, wrapper);
					try
					{
						wrapper.Invoke(evt);
					}
					catch (Exception e)
					{
						exceptions.Add(e);
					}
				}

			return exceptions;
		}

	#endregion
	}
}

#region Testing

//Here's a test class I used to check that all the code worked properly when I rewrote the class
// using StrideSaber.EventManagement;
// using StrideSaber.EventManagement.Events;
//
// //ReSharper disable all
// namespace StrideSaber.Hacks
// {
// 	/// <summary>
// 	///  A test class
// 	/// </summary>
// 	public sealed class TestClass
// 	{
// 		[EventMethod(typeof(TestEvent))]
// 		#pragma warning disable CA1822
// 		private void Fail_NonStatic()
// 		{
// 		}
// 		#pragma warning restore CA1822
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static void Fail_TooManyParams(object o1, object o2, object o3)
// 		{
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static void Fail_NotInheritedFromEvent(object o1)
// 		{
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static void Success_VoidNoParams()
// 		{
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static object? Success_ObjectReturnNoParams()
// 		{
// 			return null;
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static void Fail_CannotCastAttributeToParam(GameLoadEvent e)
// 		{
// 		}
// 		
// 		[EventMethod(typeof(Event))]
// 		private static void Fail_CannotCastReverse(GameLoadEvent e)
// 		{
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static void Success_Void_BaseEventParam(Event e)
// 		{
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static void Success_Void_InheritedEventParam(TestEvent e)
// 		{
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static object? Success_ReturnObject_BaseEventParam(Event e)
// 		{
// 			return null;
// 		}
//
// 		[EventMethod(typeof(TestEvent))]
// 		private static string? Success_ReturnObject_InheritedEventParam(TestEvent e)
// 		{
// 			return null;
// 		}
// 	}
// }

#endregion