using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace StrideSaber.Core.Logging.Enrichers
{
		/// <inheritdoc />
		[UsedImplicitly]
		public sealed class CallerContextEnricher : ILogEventEnricher
		{
			private const string CallerContextPropertyName = "CallerContext";

			/// <summary>
			///  A <see cref="StringBuilder" /> used to build the caller context
			/// </summary>
			/// <remarks>
			///  I decided to use a <see cref="ThreadLocal{T}" /> to ensure it was thread-safe.
			///  I could have used locks/synchronization, but I'm pretty sure it would have been slower and harder to read.
			///  Locks can be completely avoided because each <see cref="Thread" /> has it's own copy, and they shouldn't be able to access each other's,
			///  so there is no chance a thread can modify another thread's reference in any way
			/// </remarks>
			private static readonly ThreadLocal<StringBuilder> Builders = new(() => new StringBuilder(100), true);

			/// <inheritdoc />
			public void Enrich([NotNull] LogEvent logEvent, [NotNull] ILogEventPropertyFactory propertyFactory)
			{
				//Do add if absent so that people can override it if they want
				logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(CallerContextPropertyName, GetSourceContextString()));
			}

			[NotNull]
			private static string GetSourceContextString()
			{
				EnhancedStackTrace? trace = StackTraceEnricher.GetStackTrace();

				if (trace is null) return "<StackTrace Error>";

				var frame = (EnhancedStackFrame) trace.GetFrame(0);

				Type? type = frame.MethodInfo.DeclaringType;
				return type is null
						/* If the type is null it belongs to a module not a class (I guess a 'global' function?)
						 * From https://stackoverflow.com/a/35266094
						 * If the MemberInfo object is a global member
						 * (that is, if it was obtained from the Module.GetMethods method, which returns global methods on a module),
						 * the returned DeclaringType will be null.
						 */
						? "<Module>"
						//A plain old method belonging to a type
						: Builders.Value!
								.Clear()
								.AppendTypeDisplayName(type, false, true)
								.ToString();
		}
	}
}