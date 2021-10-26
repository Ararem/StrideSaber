using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.Concurrent;
#pragma warning disable 1591

namespace StrideSaber.Hacks
{
	public sealed class PropertyLengthTrackerEnricher : ILogEventEnricher
	{
		public readonly ConcurrentDictionary<string, int> Lengths = new();

		/// <inheritdoc />
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			foreach ((string key, LogEventPropertyValue? value) in logEvent.Properties)
			{
				int currentLength = value.ToString().Length;
				Lengths.AddOrUpdate(
						key,
						static (_, current) => current,
						static (_, current, existing) => Math.Max(current, existing),
						currentLength
				);
			}
		}
	}
}