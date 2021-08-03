using JetBrains.Annotations;
using Serilog.Core;
using Serilog.Debugging;
using Serilog.Events;
using System;
using System.Diagnostics;
using System.Reflection;

namespace StrideSaber.Core.Logging.Enrichers
{
		[UsedImplicitly]
		public sealed class StackTraceEnricher : ILogEventEnricher
		{
			private const string StackTracePropertyName = "StackTrace";

			/// <inheritdoc />
			public void Enrich([NotNull] LogEvent logEvent, [NotNull] ILogEventPropertyFactory propertyFactory)
			{
				logEvent.AddOrUpdateProperty(propertyFactory.CreateProperty(StackTracePropertyName, GetStackTraceString()));
			}

			[NotNull]
			private static string GetStackTraceString()
			{
				// return GetStackTrace()?.ToString() ?? "<Error getting stacktrace>";

				// string s = new string('=', 10);
				// return $"\n{s} STACKTRACE START {s}\n{GetStackTrace()?.ToString() ?? "<Error getting stacktrace>"}\n{s}  STACKTRACE  END  {s}\n";

				string s = new string('=', 32);
				return $"\n{s}\n{GetStackTrace()?.ToString() ?? "<Error getting stacktrace>"}\n{s}";
			}

			[CanBeNull]
			// ReSharper disable once CognitiveComplexity
			internal static EnhancedStackTrace? GetStackTrace()
			{
				/*
				 * Example output:
				 * This >>>>		at void Core.Logging.StackTraceEnricher.GetStackTrace() in C:/Users/XXXXX/Documents/Projects/Unity/Team-Defense/Assets/Scripts/Core/Logging/StackTraceEnricher.cs:line 149
				 * 					at void Serilog.Enrichers.FunctionEnricher.Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
				 * 					at void Serilog.Core.Enrichers.SafeAggregateEnricher.Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
				 * 					at void Serilog.Core.Logger.Dispatch(LogEvent logEvent)
				 * 					at void Serilog.Core.Logger.Write(LogEventLevel level, Exception exception, string messageTemplate, params object[] propertyValues)
				 * 					at void Serilog.Core.Logger.Write(LogEventLevel level, string messageTemplate, params object[] propertyValues)
				 * 					at void Serilog.Core.Logger.Write(LogEventLevel level, string messageTemplate)
				 * 					at void Serilog.Log.Write(LogEventLevel level, string messageTemplate)
				 * 					at void Serilog.Log.Information(string messageTemplate)
				 * Logged >>>		at void Testing.TestBehaviour.Test() in C:/Users/XXXXX/Documents/Projects/Unity/Team-Defense/Assets/Scripts/Testing/TestBehaviour.cs:line 43
				 *
				 * Here we can see we need to skip 9 frames to avoid including the serilog stuff.
				 */

				//Find how far we need to go to skip all the serilog methods
				var frames = new StackTrace().GetFrames();

				bool gotToSerilogYet = false;
				int skip = 0;
				//Yes this is very complicated but I don't know how to make it easier to read
				//I essentially just used a truth table and then imported it as if/else statements
				//Of course, I forgot to save the table but hey, it works!
				//Also ignore the commented duplicate if/else statements, they're just how it was before resharper did it's "truth analysis" and got rid of the impossible branches
				for (int i = 0; i < frames.Length; i++)
				{
					MethodBase? method = frames[i].GetMethod();
					//If the method, type or name is null, the expression is false
					bool isSerilog = method?.DeclaringType?.FullName?.ToLower().Contains("serilog") ?? false;
					//E.g. "at void Core.Logger.ReinitialiseLogger()+GetStackTrace()"
					// ReSharper disable once ConvertIfStatementToSwitchStatement
					if (!gotToSerilogYet && !isSerilog)
						continue;
					//E.g. "at void Serilog.Enrichers.FunctionEnricher.Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)"
					// if (!gotToSerilogYet && isSerilog)
					if (!gotToSerilogYet)
					{
						gotToSerilogYet = true;
						continue;
					}

					//E.g. "at void Serilog.Core.Logger.Dispatch(LogEvent logEvent)"
					// if (gotToSerilogYet && isSerilog) continue;
					if (isSerilog) continue;

					//E.g. "at void Testing.TestBehaviour.Test()"
					// if (gotToSerilogYet && !isSerilog)
					//Finally found the right number
					skip = i;
					break;
				}

				try
				{
					return new EnhancedStackTrace(new StackTrace(skip));
				}
				catch (Exception e)
				{
					SelfLog.WriteLine("Error creating EnhancedStackTrace: {0}", e);
					return null;
				}
			}
		}
}