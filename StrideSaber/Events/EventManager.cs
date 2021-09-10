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

namespace StrideSaber.Events
{
	/// <summary>
	/// A manager class that manages all <see cref="Event"/>s for the program
	/// </summary>
	public static class EventManager
	{
		/// <summary>
		/// The map of methods to their events. Use a <see cref="Type"/> (that inherits from <see cref="Event"/>) as the key to access all methods subscribed to that type of event.
		/// </summary>
		private static readonly ConcurrentDictionary<Type, ConcurrentHashSet<Action>> EventMethods = new();

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
			foreach (Assembly assembly in AssemblyManager.GetAllAssemblies())
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
						if (attributes.Length == 0)
						{
							//Yeah no I'm not logging this
							//This single line gives me 4-minute startup times and several gig memory allocations
							//Log.Verbose("Method {Method} is not marked as event method", method);
							continue;
						}

						if (method.IsStatic == false)
						{
							Log.Verbose("Method {Method} is non-static (instance)", method);
							invalidMethodsCount++;
							continue; //Skip to the next method
						}

						if (method.ReturnType != typeof(void))
						{
							Log.Verbose("Method {Method} is non-void", method);
							invalidMethodsCount++;
							continue;
						}

						if (method.GetParameters().Length != 0)
						{
							Log.Verbose("Method {Method} requires parameters", method);
							invalidMethodsCount++;
							continue;
						}

						var eventTypes = attributes.Select(a => a.EventType).ToArray();
						Log.Verbose("Method {Method} has {Count} event target attributes", method, attributes.Length);
						actualMethodsCount++;
						Action action = method.CreateDelegate<Action>();
						foreach (Type eventType in eventTypes)
						{
							if (type.IsAssignableTo(typeof(Event)))
							{
								AddMethod(eventType, action);
								duplicateMethodsCount++;
							}
							else
							{
								Log.Warning("Invalid event type ({Type}) for method (Does not inherit from Event)", eventType);
								invalidMethodsCount++;
							}
						}
					}
					typeScanCount++;
				}
				assemblyScanCount++;
			}

			sw.Stop();
			Log.Debug("Indexed event methods in {Elapsed:g}", sw.Elapsed);
			Log.Debug("Scanned {AssembliesCount:n0} assemblies, {TypesCount:n0} types, {MethodsCount:n0} methods", assemblyScanCount, typeScanCount, methodScanCount);
			Log.Debug("Total method count: {ActualMethodsCount} (distinct) subscribed, {DuplicateMethodsCount} (duplicate) subscribed, {InvalidMethodsCount} invalid", actualMethodsCount, duplicateMethodsCount, invalidMethodsCount);
			Log.Debug("Total: {TypeCount} event types, {MethodCount} methods", EventMethods.Count, EventMethods.Values.Sum(s => s.Count));
		}

		private static void AddMethod(Type eventType, Action action)
		{
			Log.Verbose("Adding method {Method} for event {Event}", action, eventType);
			//I don't know how to explain this, but I'll try
			//Here, we ensure that the dictionary has a set for us to store actions in.
			//If the event type already has a set to store in, that's what is returned, otherwise we make a new one.
			//I'm passing in a delegate instead of instantiating a new set so that we only allocate memory if we actually need it
			var set = EventMethods.GetOrAdd(eventType, _ => new ConcurrentHashSet<Action>());
			//Now add the action into the bag
			set.Add(action);
		}

		public static void FireEvent<T>(T evt) where T : Event
		{
			if (evt.FiringLogLevel is { } level) Log.Write(level, "Fired event {EventId}: {Event}", evt.Id, evt);
		}
	}
}