using LibEternal.Logging.Destructurers;
using LibEternal.Logging.Enrichers;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using Stride.Core.Diagnostics;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using System;
using System.Reflection;

namespace StrideSaber.Logging
{
	/// <summary>
	/// The static class that controls logging for the app
	/// </summary>
	public static class Logger
	{
		/// <summary>
		/// The minimum level of <see cref="LogEvent">LogEvents</see> that will be logged
		/// </summary>
		private const LogEventLevel MinimumLogEventLevel = LogEventLevel.Verbose;

		// ReSharper disable once UnusedMember.Local
		/// <summary>
		///	A message template that prints lots of information to help with debugging
		/// </summary>
		private const string DebugTemplate =
				"[{Timestamp:HH:mm:ss} #{EventNumber} {Level:t3}]	[{ThreadName} #{ThreadId} ({ThreadType})]	[{CallerContext}]:	{Message:lj}{NewLine}{Exception}{StackTrace}{NewLine}{NewLine}";

		/// <summary>
		/// A message template that prints simple information
		/// </summary>
		private const string SimpleTemplate =
				"[{Timestamp:HH:mm:ss} #{EventNumber}]	[{ThreadName} #{ThreadId}]	[{CallerContext}/{Level:t3}]:	{LevelIndent}{Message:lj}{NewLine}{Exception}";

		/// <summary>
		/// Here so I can guarantee thread-safety when init-ing/shutting down
		/// </summary>
		private static readonly object Lock = new();

		/// <summary>
		/// Stores if the logger has been initialized yet
		/// </summary>
		private static bool initialized = false;

		/// <summary>
		/// Initializes the logger
		/// </summary>
		internal static void Init()
		{
			lock (Lock)
			{
				if (initialized) return;
				HookAndDisableStrideDefaultLogger(null);

				LoggerConfiguration? config = new LoggerConfiguration()
						.MinimumLevel.Is(MinimumLogEventLevel)
						.Enrich.With<CallerContextEnricher>()
						.Enrich.With<StackTraceEnricher>()
						.Enrich.With<DemystifiedExceptionsEnricher>()
						.Enrich.With<LogEventNumberEnricher>()
						.Enrich.With<EventLevelIndentEnricher>()
						.Enrich.With<ThreadInfoEnricher>()
						.Enrich.FromLogContext()
						.Destructure.With<DelegateDestructurer>()
						.Destructure.With<StrideObjectDestructurer>();

				//Switch the template depending on if we are debugging
				// ReSharper disable once InlineTemporaryVariable
#if DEBUG && false
			const string template = DebugTemplate;
#else
				const string template = SimpleTemplate;
#endif
				Log.Logger = config
						// .WriteTo.Async(writeTo => writeTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Literate))
						.WriteTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Literate)
						.CreateLogger();

				Log.Information("Logger initialized");
				initialized = true;
			}
		}

		/// <summary>
		/// A little method that fixes the stride logging system so that it only does what I want it to do
		/// </summary>
		//As to why I have the method hooked up to an event, it's because Stride actually hooks up it's logger in the constructor for the Game
		/*
    public Game()
    {
      this.logListener = this.GetLogListener();
      if (this.logListener != null)
=====>   GlobalLogger.GlobalMessageLogged += (Action<ILogMessage>) this.logListener;
      ...
    }
		 */
		//Which means I have to call this after the constructor, so we use an event instead of doing it manually
		[EventMethod(typeof(GameLoadEvent))]
		private static void HookAndDisableStrideDefaultLogger(Event? _)
		{
			Log.Information("Hooking and disabling stride logging system");
			//TODO: Instead of clearing just removing the annoying stride part
			//Here I'm clearing the event because stride sets up it's own handler which I don't want
			//(Otherwise you would get duped logs when debugging which is annoying)
			//This is because by default stride logs to the console, but I'm doing that myself
			typeof(GlobalLogger).GetField(nameof(GlobalLogger.GlobalMessageLogged), BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);
			//Hook our logger up to stride's system
			GlobalLogger.GlobalMessageLogged += GlobalLogger_OnGlobalMessageLogged;
			Log.Information("Stride log redirection complete");
		}

		/// <summary>
		/// Called by the <see cref="GlobalLogger"/> when ever a message is logged by stride
		/// </summary>
		/// <remarks>Just passes it through to Serilog</remarks>
		/// <param name="msg">The log message to log</param>
		private static void GlobalLogger_OnGlobalMessageLogged(ILogMessage msg)
		{
			//Convert it to serilog enum
			LogEventLevel level = msg.Type switch
			{
					LogMessageType.Debug => LogEventLevel.Debug,
					LogMessageType.Verbose => LogEventLevel.Verbose,
					LogMessageType.Info => LogEventLevel.Information,
					LogMessageType.Warning => LogEventLevel.Warning,
					LogMessageType.Error => LogEventLevel.Error,
					LogMessageType.Fatal => LogEventLevel.Fatal,
					_ => throw new Exception("How the fuck did we get here?")
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