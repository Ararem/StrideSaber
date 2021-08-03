using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using Stride.Core.MicroThreading;
using System.Threading;
using ThreadPool = Stride.Core.Threading.ThreadPool;

namespace StrideSaber.Core.Logging.Enrichers
{
		/// <inheritdoc />
		/// <summary>
		/// Enriches log events with information about the <see cref="System.Threading.Thread.CurrentThread" />
		/// </summary>
		[UsedImplicitly]
		public sealed class ThreadInfoEnricher : ILogEventEnricher
		{
			private const string ThreadNamePropertyName = "ThreadName";
			private const string ThreadIdPropertyName = "ThreadId";

			/// <summary>
			/// What type of thread we're currently in e.g. Thread pool, user, micro thread etc
			/// </summary>
			public const string ThreadTypePropertyName = "ThreadType";

			/// <inheritdoc />
			public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
			{
				Thread curr = Thread.CurrentThread;
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(ThreadNamePropertyName, curr.Name ?? "Unnamed Thread"));
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(ThreadIdPropertyName, curr.ManagedThreadId));
				string threadType;
				if (MicroThread.Current is not null)
					threadType = "MicroThread";
				else if (curr.IsThreadPoolThread)
					threadType = "Dotnet Pool";
				else if (ThreadPool.IsWorkedThread)
					threadType = "Stride Pool";
				//Default is user thread
				else
					threadType = "User Thread";
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(ThreadTypePropertyName, threadType));
			}
		}
	}