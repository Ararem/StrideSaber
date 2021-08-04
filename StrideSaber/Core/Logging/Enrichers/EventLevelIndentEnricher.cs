using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using StrideSaber.Core.ObjectPools;
using System.Collections.Generic;
using System.Text;

namespace StrideSaber.Core.Logging.Enrichers
{
		/// <inheritdoc />
		/// <summary>
		///  Enriches an <see cref="Serilog.Events.LogEvent" /> with an indent depending upon the <see cref="Serilog.Events.LogEventLevel" /> of the log event.
		/// </summary>
		[UsedImplicitly]
		public sealed class EventLevelIndentEnricher : ILogEventEnricher
		{
			private const string EventLevelIndentPropertyName = "LevelIndent";

			private const string IndentString = "\t";

			private static readonly Dictionary<LogEventLevel, string> IndentLevels = new()
			{
					//I know it's kind of pointless to pass in 0 instead of making it simply an empty string, but it does make it easier to change in the future (only 1 char)
					[LogEventLevel.Fatal] = GenerateIndentString(0),
					[LogEventLevel.Error] = GenerateIndentString(0),
					[LogEventLevel.Warning] = GenerateIndentString(0),
					[LogEventLevel.Information] = GenerateIndentString(0),
					[LogEventLevel.Debug] = GenerateIndentString(1),
					[LogEventLevel.Verbose] = GenerateIndentString(2)
			};

			/// <inheritdoc />
			public void Enrich([NotNull] LogEvent logEvent, [NotNull] ILogEventPropertyFactory propertyFactory)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(EventLevelIndentPropertyName, IndentLevels[logEvent.Level]));
			}

			/// <summary>
			///  Just repeats the <see cref="IndentString" /> by the amount of <paramref name="repetitions" />
			/// </summary>
			/// <param name="repetitions">The number of times to repeat the indent string</param>
			[NotNull]
			private static string GenerateIndentString(int repetitions)
			{
				//Get a builder from the pool and make sure it's big enough
				StringBuilder sb = StringBuilderPool.Instance.Get();
				sb.EnsureCapacity(IndentString.Length * repetitions);
				//Just repeat the string and return it
				string s = sb.Insert(0, IndentString, repetitions).ToString();
				StringBuilderPool.Instance.Return(sb);
				return s;

				// Easier to read but waay less efficient
				// return string.Concat(Enumerable.Repeat(IndentString, repetitions));
			}
		}
}