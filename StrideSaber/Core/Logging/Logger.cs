using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Stride.Core.Diagnostics;
using System;

namespace StrideSaber.Core.Logging
{
	public static partial class Logger
	{
		private const LogEventLevel MinimumLogEventLevel = LogEventLevel.Verbose;

		// ReSharper disable once UnusedMember.Local
		private const string DebugTemplate =
				"[{Timestamp:HH:mm:ss}#{EventNumber} {Level:t3}] [{ThreadName} #{ThreadId} ({ThreadType})]	[{CallerContext}]: 	{Message:lj}{NewLine}{Exception}{StackTrace}{NewLine}{NewLine}";

		private const string SimpleTemplate =
				"[{Timestamp:HH:mm:ss} {Level:t3}] [{ThreadName} #{ThreadId} ({ThreadType})] [{CallerContext}]:	{LevelIndent}{Message:lj}{NewLine}{Exception}";

		/// <summary>
		/// Here so I can guarantee thread-safety when init-ing/shutting down
		/// </summary>
		private static readonly object Lock = new();
		private static bool initialized = false;
		/// <summary>
		/// Initializes the logger
		/// </summary>
		internal static void Init()
		{
			lock (Lock)
			{
				if (initialized) return;
				LoggerConfiguration? config = new LoggerConfiguration()
						.MinimumLevel.Is(MinimumLogEventLevel)
						.Enrich.With<CallerContextEnricher>()
						.Enrich.With<StackTraceEnricher>()
						.Enrich.With<DemystifiedExceptionsEnricher>()
						.Enrich.With<LogEventNumberEnricher>()
						.Enrich.With<EventLevelIndentEnricher>()
						.Enrich.With<ThreadInfoEnricher>()
						.Enrich.FromLogContext();

				//Switch the template depending on if we are debugging
				// ReSharper disable once InlineTemporaryVariable
#if DEBUG && false
			const string template = DebugTemplate;
#else
				const string template = SimpleTemplate;
#endif
				Log.Logger = config
						.WriteTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Code)
						.CreateLogger();

				//Hook our logger up to stride's system
				GlobalLogger.GlobalMessageLogged += GlobalLogger_OnGlobalMessageLogged;

				Log.Information("Logger initialized");
				initialized = true;
			}
		}

		/// <summary>
		/// Called by the <see cref="GlobalLogger"/> when ever a message is logged by stride
		/// </summary>
		/// <remarks>Just passes it through to Serilog</remarks>
		/// <param name="msg">The log message to log</param>
		private static void GlobalLogger_OnGlobalMessageLogged(ILogMessage msg)
		{
			//Convert it to serilog enum
#pragma warning disable 8509
			LogEventLevel level = msg.Type switch
#pragma warning restore 8509
			{
					LogMessageType.Debug => LogEventLevel.Debug,
					LogMessageType.Verbose => LogEventLevel.Verbose,
					LogMessageType.Info => LogEventLevel.Information,
					LogMessageType.Warning => LogEventLevel.Warning,
					LogMessageType.Error => LogEventLevel.Error,
					LogMessageType.Fatal => LogEventLevel.Fatal,
			};
			Log.ForContext("CallerContext", "S3D::" + msg.Module) //We change the caller context property so we know it's from stride
					.Write(level, ExceptionInfoToNormal(msg.ExceptionInfo), "{Message}", msg.Text);

			static Exception? ExceptionInfoToNormal(ExceptionInfo? info)
			{
				return info is null ? null : new Exception(info.ToString());
			}
		}

		/// <summary>
		/// Shuts down the logger (and cleans up)
		/// </summary>
		internal static void Shutdown()
		{
			//Synchronize...
			lock (Lock)
			{
				if (!initialized) return;
				Log.Information("Logger shutting down");
				Log.CloseAndFlush();
				initialized = false;
			}
		}
	}
}