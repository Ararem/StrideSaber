using JetBrains.Annotations;
using System;
using System.Reflection;

namespace StrideSaber.EventManagement
{
	/// <summary>
	/// A struct that stores statistical information about method indexing
	/// </summary>
	/// <seealso cref="EventManager.IndexEventMethods"/>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public struct MethodIndexingStats
	{
		/// <summary>
		/// The number of <see cref="Assembly">Assemblies</see> that were found and scanned
		/// </summary>
		public uint AssemblyScanCount;
		/// <summary>
		/// The number of <see cref="Type">Types</see> that were found and scanned
		/// </summary>
		public uint TypeScanCount;
		/// <summary>
		/// The number of <see cref="MethodInfo">Methods</see> that were found and scanned. This includes methods that were invalid or were marked for several event types
		/// </summary>
		public uint MethodScanCount;

		/// <summary>
		/// How many methods were found that were invalid (e.g. instance, too many parameters, generic, etc). If a method declaration is valid (e.g. static, void, no params), but has one valid event target and one invalid target, this will be incremented once per each invalid target
		/// </summary>
		public uint InvalidMethodCount;
		/// <summary>
		/// How many methods were found, counting each method once, even if it was marked for multiple event types (e.g. 3 methods marked twice = 3)
		/// </summary>
		public uint ActualMethodCount;
		/// <summary>
		/// How many methods were found, counting once per valid event type (e.g. 3 methods marked twice = 6)
		/// </summary>
		public uint DuplicateMethodCount;
	}
}