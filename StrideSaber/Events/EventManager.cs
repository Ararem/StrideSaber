using Serilog;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace StrideSaber.Events
{
	public static class EventManager
	{
		/// <summary>
		/// The cache of already-instantiated events
		/// </summary>
		private static readonly ConcurrentDictionary<Type, ReusableEvent> EventInstanceCache = new();

		/// <summary>
		/// The map of methods to their events
		/// </summary>
		private static readonly ConcurrentDictionary<Type, Action> EventMethods = new();

		internal static void Init()
		{
			Log.Information("Initializing event manager");

			IndexEventMethods();
		}

		private static void IndexEventMethods()
		{
			Log.Debug("Indexing event methods");
			var sw = Stopwatch.StartNew();

			

			sw.Stop();
			Log.Debug("Indexed event methods in {Elapsed:ss'.'FF}", sw.Elapsed);
		}
	}
}