using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using System.Threading;

namespace StrideSaber.Core.Logging.Enrichers
{
		/// <inheritdoc />
		/// <summary>
		///  Enriches log events with a counter that counts how many log events have been logged.
		/// </summary>
		/// <remarks>
		///  This class is <see cref="System.Threading.Thread" /> safe, but contains (and modifies) global state, so use with multiple
		///  <see cref="Serilog.ILogger" />s is not supported
		/// </remarks>
		[UsedImplicitly]
		public sealed class LogEventNumberEnricher : ILogEventEnricher
		{
			private const string LogEventNumberPropertyName = "EventNumber";

			private static long counter = 0;

			/// <inheritdoc />
			public void Enrich([NotNull] LogEvent logEvent, [NotNull] ILogEventPropertyFactory propertyFactory)
			{
				//Increment our counter in a 'thread safe' manner
				long c = Interlocked.Increment(ref counter);
				LogEventProperty property = propertyFactory.CreateProperty(LogEventNumberPropertyName, c);
				logEvent.AddOrUpdateProperty(property);
			}
	}
}