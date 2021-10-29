using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using LibEternal.Logging.Enrichers;
using Serilog;

namespace StrideSaber.Benchmarks
{
	/// <summary>
	/// Benchmarks for <see cref="CallerContextEnricher"/>
	/// </summary>
	[SimpleJob, MemoryDiagnoser, PublicAPI]
	// ReSharper disable once ClassCanBeSealed.Global
	public class CallerContextEnricherPerfHacksBenchmarks
	{
		/// <summary>
		/// What performance mode is being benchmarked
		/// </summary>
		[ParamsAllValues]
		public CallerContextEnricher.PerfMode PerfMode;

		private ILogger logger = null!;

		/// <summary>
		/// Sets up the logger
		/// </summary>
		[GlobalSetup]
		public void Setup()
		{
			logger = new LoggerConfiguration().Enrich.With(new CallerContextEnricher(PerfMode)).CreateLogger();
		}

		/// <summary>
		/// Runs the actual benchmark code
		/// </summary>
		[Benchmark]
		public void Run()
		{
			logger.Information("Test Message");
		}
	}
}