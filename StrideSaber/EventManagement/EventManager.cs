using ConcurrentCollections;
using Serilog;
using Serilog.Context;
using Stride.Core.Extensions;
using StrideSaber.Modding;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace StrideSaber.EventManagement
{
	/// <summary>
	/// A manager class that manages all <see cref="Event">events</see> for the program
	/// </summary>
	public static class EventManager
	{
		/// <summary>
		/// The map of methods to their events. Use a <see cref="Type"/> (that inherits from <see cref="Event"/>) as the key to access all methods subscribed to that type of event.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, ConcurrentHashSet<Action<Event>>> EventMethods = new();

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
			//These numbers are for how many assemblies/types/methods we scanned
			//This includes ones we skipped/were invalid
			uint assemblyScanCount = 0, typeScanCount = 0, methodScanCount = 0;
			//These are method counters
			//invalidMethodsCount is how many methods we found that were invalid (e.g. instance, non-void etc)
			//actualMethodsCount is how many methods we found, not including methods that were subscribed to multiple events (e.g. 3 methods marked twice = 3)
			//duplicateMethodsCount is how many methods we found, including duplicates (e.g. 3 methods marked twice = 6)
			uint invalidMethodsCount = 0, actualMethodsCount = 0, duplicateMethodsCount = 0;
			Log.Debug("Indexing event methods");
			//Clear the event methods. To avoid wasting memory, just reuse the old hashsets we already have, but remove any items from them
			//(Instead of removing all the sets from the dictionary and wasting all that precious memory)
			EventMethods.Values.ForEach(s => s.Clear());
			var sw = Stopwatch.StartNew();

			//Loop through all the assemblies we have
			foreach (Assembly assembly in AssemblyManager.GetAllExternalAssemblies())
			{
				//For debugging purposes
				using IDisposable _ = LogContext.PushProperty("Assembly", assembly);
				Log.Debug("Scanning assembly {Assembly}", assembly);

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

				foreach (Type type in types)
				{
					using IDisposable __ = LogContext.PushProperty("Type", type);
					Log.Verbose("Scanning type {Type}", type);
					var methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					//I'm SICK OF ALL THESE GODDAMN LOOPS!!!!!!!!!!!!!!
					foreach (MethodInfo method in methods)
					{
						methodScanCount++;
						var attributes = method.GetCustomAttributes<EventMethodAttribute>().ToArray();
						if (attributes.Length == 0) continue;

						if (method.IsStatic == false)
						{
							Log.Verbose("Method {$Method} is non-static (instance)", method);
							invalidMethodsCount++;
							continue; //Skip to the next method
						}

						var methodParams = method.GetParameters();
						if (methodParams.Length != 1)
						{
							Log.Verbose("Method {$Method} has invalid parameter count ({ParameterCount})", method, methodParams.Length);
							invalidMethodsCount++;
							continue;
						}

						//TODO: Allow for 0 params
						ParameterInfo parameter = methodParams[0];
						if (parameter.ParameterType != typeof(Event))
						{
							Log.Verbose("Method {$Method} has incorrect parameter type ({Type})", method, parameter.ParameterType);
							invalidMethodsCount++;
							continue;
						}

						var eventTypes = attributes.Select(a => a.EventType).ToArray();
						Log.Verbose("Method {$Method} has {Count} event target attributes", method, attributes.Length);
						Action<Event> action;
						//Methods that return void are easy for us
						if (method.ReturnType == typeof(void))
						{
							action = method.CreateDelegate<Action<Event>>();
						}
						//For a non-void method, we need to wrap the function into an action that ignores the result
						else
						{
							Func<Event, object> methodFunc = method.CreateDelegate<Func<Event, object>>();
							action = evt => methodFunc(evt);
						}

						//True/false if at least one of the event attributes on the method was valid
						//Only reason it's needed is because otherwise the tracking numbers are funky and don't match what actually happens
						bool success = false;
						foreach (Type eventType in eventTypes)
							if (eventType.IsAssignableTo(typeof(Event)))
							{
								AddMethod(eventType, action);
								success = true;
								duplicateMethodsCount++;
							}
							else
							{
								Log.Warning("Invalid event type ({Type}) for method {$Method} (Does not inherit from {EventBaseType})", eventType, method, typeof(Event));
								invalidMethodsCount++;
							}

						if (success)
							actualMethodsCount++;
					}

					typeScanCount++;
				}

				assemblyScanCount++;
			}

			sw.Stop();
			Log.Debug("Indexed event methods in {Elapsed:g}", sw.Elapsed);
			Log.Debug("Scanned {AssembliesCount:n0} assemblies, {TypesCount:n0} types, {MethodsCount:n0} methods", assemblyScanCount, typeScanCount, methodScanCount);
			Log.Debug("Total method count: {ActualMethodsCount:n0} (distinct) subscribed, {DuplicateMethodsCount:n0} (duplicate) subscribed, {InvalidMethodsCount:n0} invalid", actualMethodsCount, duplicateMethodsCount, invalidMethodsCount);
			Log.Debug("Total: {TypeCount:n0} event types, {MethodCount:n0} methods", EventMethods.Count, EventMethods.Values.Sum(s => s.Count));
		}

		private static void AddMethod(Type eventType, Action<Event> action)
		{
			Log.Verbose("Adding method {@Method} for event {Event}", action, eventType);
			//I don't know how to explain this, but I'll try
			//Here, we ensure that the dictionary has a set for us to store actions in.
			//If the event type already has a set to store in, that's what is returned, otherwise we make a new one.
			//I'm passing in a delegate instead of instantiating a new set so that we only allocate memory if we actually need it
			var set = EventMethods.GetOrAdd(eventType, _ => new ConcurrentHashSet<Action<Event>>());
			//Now add the action into the bag
			set.Add(action);
		}

		//TODO: Make the event methods be able to handle a more specific/inherited type than plain old Event
		/// <summary>
		/// Fires (invokes) an <see cref="Event"/> of type <typeparamref name="T"/>
		/// </summary>
		/// <param name="evt">The <see cref="Event"/> to fire</param>
		public static void FireEvent<T>(T evt) where T : Event
		{
			EventMethods.TryGetValue(typeof(T), out var methods);
			bool log = evt.FiringLogLevel is not null;
			//TODO: Yeah how thread-safe is this?
			if (log) Log.Write(evt.FiringLogLevel!.Value, "Firing event {EventId} for {Count} subscribers: {Event}", evt.Id, methods?.Count ?? 0, evt);
			if (methods != null)
			{
				using IDisposable _ = LogContext.PushProperty("Event", evt);
				foreach (Action<Event> m in methods)
				{
					if (log) //I only want this to be logged in very rare circumstances
						Log.Verbose("[{EventId}]: Invoking method {@Method}", evt.Id, m);
					m.Invoke(evt);
				}
			}
		}
	}
}