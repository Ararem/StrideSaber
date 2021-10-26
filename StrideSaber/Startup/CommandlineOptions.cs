using CommandLine;
using Serilog.Events;
using SmartFormat;
using Stride.Core;
using System;
using System.Collections.Generic;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

namespace StrideSaber.Startup
{
	/// <summary>
	/// A record class that is used as the base for command-line option parsing
	/// </summary>
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

		/// <summary>
		/// If the default splash screen should be disabled
		/// </summary>
		[Option('s', nameof(NoSplash), Default = false, HelpText = "A flag that disables the splash screen")]
		public bool NoSplash { get; init; }

		/// <summary>
		/// The minimum time allowed between frames
		/// </summary>
		/// <remarks>Mutually exclusive with <see cref="MaxFps"/></remarks>
		/// <seealso cref="MaxFps"/>
		/// <seealso cref="ThreadThrottler.MinimumElapsedTime"/>
		[Option('t', nameof(MinimumFrameTime), SetName = "MaxFps", Default = null, HelpText = "The minimum amount of time each frame should take")]
		public TimeSpan? MinimumFrameTime { get; init; }

		/// <summary>
		/// The maximum framerate the application can run at
		/// </summary>
		/// <remarks>Mutually exclusive with <see cref="MinimumFrameTime"/></remarks>
		/// <seealso cref="MinimumFrameTime"/>
		/// <seealso cref="ThreadThrottler.SetMaxFrequency"/>
		[Option('f', nameof(MaxFps), SetName = "MaxFps", Default = null, HelpText = "The maximum amount of frames that should be rendered/displayed per second")]
		public ushort? MaxFps { get; init; }

		/// <summary>
		/// Whether VSync should be enabled
		/// </summary>
		[Option('v', nameof(VSync), Default = false, HelpText = "Whether VSync should be enabled")]
		public bool VSync { get; init; }
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

		/// <summary>
		/// Runs the secret test command (don't tell anyone about this ok?)
		/// </summary>
		/// <!--The first easter egg I ever made-->
		[Option(nameof(RunTestCommand), Hidden = true, HelpText = "Runs the secret test command")]
		public bool RunTestCommand { get; init; }
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