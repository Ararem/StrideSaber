using BenchmarkDotNet.Attributes;
using LibEternal.Logging.Enrichers;
using Serilog;
using static LibEternal.Logging.Enrichers.CallerContextEnricher.PerfMode;

namespace StrideSaber.Benchmarks
{
	// ReSharper disable once ClassCanBeSealed.Global
	[SimpleJob, MemoryDiagnoser]
	public class CallerContextEnricherPerfHacksBenchmarks
	{
		[ParamsAllValues]
		public CallerContextEnricher.PerfMode PerfMode;

		private ILogger logger;

		[GlobalSetup]
		public void Setup()
		{
			logger = new LoggerConfiguration().Enrich.With(new CallerContextEnricher(PerfMode)).CreateLogger();
		}

		[Benchmark]
		public void Run()
		{
			logger.Information("Test Message");
		}
	}
}