using LibEternal.Logging.Destructurers;
using LibEternal.Logging.Enrichers;
using LibEternal.ObjectPools;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;
using SmartFormat;
using Stride.Core;
using Stride.Core.Diagnostics;
using StrideSaber.Diagnostics;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using StrideSaber.Extensions.SmartFormat;
using StrideSaber.Startup;
using System;
using System.Linq;
using System.Reflection;
using static LibEternal.Logging.Enrichers.CallerContextEnricher;
using static StrideSaber.Logging.StrideThreadInfoEnricher;
using static LibEternal.Logging.Enrichers.LogEventNumberEnricher;

namespace StrideSaber.Logging
{
	/// <summary>
	///  The static class that controls logging for the app
	/// </summary>
	public static class Logger
	{
		/// <summary>
		///  A message template that prints lots of information to help with debugging
		/// </summary>
		private const string DebugTemplate =
				"[{Timestamp:HH:mm:ss} {" + EventNumberProp + ",-5:'#'####} {Level:t3}] [{" + ThreadNameProp + ",-20} {" + ThreadIdProp + ",-3:'#'##} ({" + ThreadTypeProp /*Always 11 chars (from enricher)*/ + "})]\t[{" + CallingTypeProp + "}/{" + CallingMethodProp + "}]:\t{Message:l}{NewLine}{Exception}{" + StackTraceProp + "}{NewLine}{NewLine}";

		/// <summary>
		///  A message template that prints simple information
		/// </summary>
		private const string SimpleTemplate =
				"[{Timestamp:HH:mm:ss} {" + EventNumberProp + ",-5:'#'####} {Level:t3}] [{" + ThreadNameProp + ",-20} {" + ThreadIdProp + ",-3:'#'##}] [{" + CallingTypeProp + "}/{" + CallingMethodProp + "}]:\t{Message:l}{NewLine}{Exception}";

		/// <summary>
		///  Here so I can guarantee thread-safety when init-ing/shutting down
		/// </summary>
		private static readonly object Lock = new();

		/// <summary>
		///  Stores if the logger has been initialized yet
		/// </summary>
		private static bool initialized = false;

		/// <summary>
		///  Initializes the logger
		/// </summary>
		internal static void Init()
		{
			lock (Lock)
			{
				if (initialized) return;

				Serilog.Debugging.SelfLog.Enable(SelfLog);
				DefaultOptions cmdOptions = (DefaultOptions)StrideSaberApp.CmdOptions;
				Stride.Core.Diagnostics.Logger.MinimumLevelEnabled = LogMessageType.Verbose;
				LoggerConfiguration config = new LoggerConfiguration()
				                             .MinimumLevel.Is(cmdOptions.LogLevel)
				                             .Enrich.FromLogContext()
				                             .Enrich.With<DemystifiedExceptionsEnricher>()
				                             .Enrich.With<LogEventNumberEnricher>()
				                             .Enrich.With<EventLevelIndentEnricher>()
				                             .Enrich.With<StrideThreadInfoEnricher>()
				                             .Destructure.AsScalar<TrackedTask>() //Want custom ToString support
				                             .Destructure.With<DelegateDestructurer>()
				                             .Destructure.With<StrideObjectDestructurer>();

				string template;
				if (cmdOptions is DebugOptions { DebugTemplate: true })
				{
					template = DebugTemplate;
					//Debug template requires stack traces
					config = config.Enrich.With(new CallerContextEnricher(PerfMode.SlowWithTrace));
				}
				else
				{
					template = SimpleTemplate;
					//No traces needed, my code go vrooooooooooooooooooooooooooooom
					config = config.Enrich.With(new CallerContextEnricher(PerfMode.FastDemystify));
				}

				if (cmdOptions.AsyncLog)
					Log.Logger = config.WriteTo.Async(writeTo => writeTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Literate)
					).CreateLogger();
				else
					Log.Logger = config
					             .WriteTo.Console(outputTemplate: template, applyThemeToRedirectedOutput: true, theme: AnsiConsoleTheme.Literate)
					             .CreateLogger();

				//Also set up the smart format stuff I like
				Smart.Default.AddExtensions(new QuotedStringFormatter());
				Log.Information("Logger initialized");
				//TODO: Maybe use `Interlocked` for this?
				initialized = true;
			}
		}

		private static void SelfLog(string message)
		{
			Console.Error.WriteLine(StringBuilderPool.BorrowInline(static (sb, msg) =>
							sb.Append("\u001b[31;1m") //Set colour to Bright Red
							  .Append(msg)
							  .Append("\u001b[0m") //Reset colour to default
					, message));
		}

		/// <summary>
		///  A little method that fixes the stride logging system so that it only does what I want it to do
		/// </summary>
		//As to why I have the method hooked up to an event, it's because Stride actually hooks up it's logger in the constructor for the Game
		//     public Game()
		//     {
		//       this.logListener = this.GetLogListener();
		//       if (this.logListener != null)
		// =====>   GlobalLogger.GlobalMessageLogged += (Action<ILogMessage>) this.logListener;
		//       ...
		//     }
		//Which means I have to call this after the constructor, so we use an event instead of doing it manually
		[EventMethod(typeof(GameLoadEvent))]
		private static void HookAndDisableStrideConsoleLogger()
		{
			//Here I'm clearing the event because stride sets up it's own handler which I don't want
			//(Otherwise you would get duped logs when debugging which is annoying)
			//This is because by default stride logs to the console, but I'm doing that myself

			Log.Information("Hooking and disabling stride console logging system");

			//Get the event info that corresponds to the event called when a message is logged (the one we want to modify)
			EventInfo? eventInfo = typeof(GlobalLogger).GetEvent(nameof(GlobalLogger.GlobalMessageLogged));
			if (eventInfo is null)
			{
				Log.Warning("Could not find event {EventName} in class {Type} to modify", nameof(GlobalLogger.GlobalMessageLogged), typeof(GlobalLogger));
				return;
			}

			//Find the action we want to remove
			const string serilogMethodName = "OnLogInternal";
			MethodInfo? toRemoveMethodInfo = typeof(LogListener).GetMethod(serilogMethodName, BindingFlags.Instance | BindingFlags.NonPublic);
			if (toRemoveMethodInfo is null)
			{
				Log.Warning("Could not find internal serilog method {MethodName} in class {Type} to modify", serilogMethodName, typeof(LogListener));
				return;
			}

			FieldInfo? backingField = typeof(GlobalLogger).GetField(nameof(GlobalLogger.GlobalMessageLogged), BindingFlags.Static | BindingFlags.NonPublic);
			if (backingField is null)
			{
				Log.Warning("Could not find backing field for event {EventName} in class {Type}", nameof(GlobalLogger.GlobalMessageLogged), typeof(GlobalLogger));
				return;
			}

			Delegate toRemoveDelegate = ((Action<ILogMessage>)backingField.GetValue(null)!)
			                            // Go through the methods added and find the one that points to the method we're trying to remove
			                            .GetInvocationList().First(d => d.Method.Equals(toRemoveMethodInfo));

			eventInfo.RemoveEventHandler(null, toRemoveDelegate);
			//Hook our logger up to stride's system
			GlobalLogger.GlobalMessageLogged += Stride_GlobalLogger;
			Log.Information("Stride log redirection complete");
		}

		/// <summary>
		///  Called by the <see cref="GlobalLogger"/> when ever a message is logged by stride
		/// </summary>
		/// <remarks>Just passes it through to Serilog</remarks>
		/// <param name="msg">The log message to log</param>
		private static void Stride_GlobalLogger(ILogMessage msg)
		{
			//Convert it to serilog enum
			LogEventLevel level = msg.Type switch
			{
					LogMessageType.Debug   => LogEventLevel.Debug,
					LogMessageType.Verbose => LogEventLevel.Verbose,
					LogMessageType.Info    => LogEventLevel.Information,
					LogMessageType.Warning => LogEventLevel.Warning,
					LogMessageType.Error   => LogEventLevel.Error,
					LogMessageType.Fatal   => LogEventLevel.Fatal,
					_                      => throw new Exception("How the fuck did we get here?")
			};
			Log.ForContext("CallerContext", "S3D::" + msg.Module) //We change the caller context property so we know it's from stride
			   .Write(level, ExceptionInfoToNormal(msg.ExceptionInfo), "{Message}", msg.Text);

			static Exception? ExceptionInfoToNormal(ExceptionInfo? info)
			{
				return info is null ? null : new Exception(info.ToString());
			}
		}

		/// <summary>
		///  Shuts down the logger (and cleans up)
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