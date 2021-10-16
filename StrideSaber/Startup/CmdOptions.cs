using CommandLine;
using JetBrains.Annotations;
using Serilog.Events;

namespace StrideSaber.Startup
{
	/// <summary>
	/// A record class that is used for command-line option parsing
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public record CmdOptions
	{
		/// <summary>
		/// The level of log events that should be logged
		/// </summary>
		[Option('l', nameof(LogLevel), Default = LogEventLevel.Verbose, HelpText = "The level of log events that should be shown")]
		public LogEventLevel LogLevel { get; init; }

		/// <summary>
		/// If the debug template should be used for log messages
		/// </summary>
		[Option('d', nameof(DebugTemplate), Default = false, HelpText = "If the debug template should be used for log messages")]
		public bool DebugTemplate { get; init; }

		/// <summary>
		/// Whether logging should be don asynchronously or on the caller thread
		/// </summary>
		[Option('a', nameof(AsyncLog), Default = false, HelpText = "Whether logging should be done asynchronously or on the caller thread.")]
		public bool AsyncLog { get; init; }
	}
}