using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using Stride.Core.MicroThreading;
using System.Threading;
using ThreadPool = Stride.Core.Threading.ThreadPool;

namespace StrideSaber.Logging
{
	/// <inheritdoc />
	/// <summary>
	/// Enriches log events with information about the <see cref="System.Threading.Thread.CurrentThread" />
	/// </summary>
	[UsedImplicitly]
	public sealed class StrideThreadInfoEnricher : ILogEventEnricher
	{
		/// <summary>
		/// The name of the property for the thread name
		/// </summary>
		public const string ThreadNameProp = "ThreadName";

		/// <summary>
		/// The name of the property for the thread ID
		/// </summary>
		public const string ThreadIdProp = "ThreadId";

		/// <summary>
		/// The name of the property for the thread type
		/// </summary>
		public const string ThreadTypeProp = "ThreadType";

		/// <inheritdoc />
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			Thread curr = Thread.CurrentThread;
			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(ThreadNameProp, curr.Name ?? "Unnamed Thread"));
			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(ThreadIdProp, curr.ManagedThreadId));
			string threadType;
			if (MicroThread.Current is not null)
				threadType = "MicroThread";
			else if (ThreadPool.IsWorkedThread)
				threadType = "Stride Pool";
			else if (curr.IsThreadPoolThread)
				threadType = "Dotnet Pool";
			//Default is user thread
			else
				threadType = "User Thread";
			logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(ThreadTypeProp, threadType));
		}
	}
}