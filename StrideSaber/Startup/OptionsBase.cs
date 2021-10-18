using CommandLine;
using JetBrains.Annotations;
using Serilog.Events;
using SmartFormat;
using System;
using System.Collections.Generic;

namespace StrideSaber.Startup
{
	/// <summary>
	/// A record class that is used as the base for command-line option parsing
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers | ImplicitUseTargetFlags.WithInheritors)]
	public abstract record OptionsBase
	{
		/// <inheritdoc />
		public override string ToString()
		{
			return Smart.Format("{0}", this);
		}
	}

	/// <summary>
	/// The options class for running the game normally
	/// </summary>
	[Verb("run", true, HelpText = "Runs the game normally")]
	public record DefaultOptions : OptionsBase
	{
		/// <summary>
		/// The level of log events that should be logged
		/// </summary>
		[Option('l', nameof(LogLevel), Default = LogEventLevel.Verbose, HelpText = "The level of log events that should be shown")]
		public LogEventLevel LogLevel { get; init; }

		/// <summary>
		/// Whether logging should be don asynchronously or on the caller thread
		/// </summary>
		[Option('a', nameof(AsyncLog), Default = false, HelpText = "Whether logging should be done asynchronously or on the caller thread.")]
		public bool AsyncLog { get; init; }
	}

	/// <summary>
	/// The verb option that indicates the game should be running in debug mode
	/// </summary>
	[Verb("debug", HelpText = "Runs the game in debugging mode")]
	public record DebugOptions : DefaultOptions
	{
		/// <summary>
		/// If the debug template should be used for log messages
		/// </summary>
		[Option('t', nameof(DebugTemplate), Default = false, HelpText = "If the debug template should be used for log messages")]
		public bool DebugTemplate { get; init; }
	}

	/// <summary>
	/// A verb option that is used for running <see cref="BenchmarkDotNet"/>
	/// </summary>
	[Verb("benchmark", HelpText = "Benchmarks the code using BenchmarkDotNet")]
	public record BenchmarkDotNetOptions : OptionsBase
	{
		/// <summary>
		/// The options that will be passed into <see cref="BenchmarkDotNet"/>
		/// </summary>
		[Value(0, HelpText = "The options to be passed into BenchmarkDotNet")]
		public IEnumerable<string>? Options { get; init; } = null!;
	}
}