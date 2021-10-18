using BenchmarkDotNet.Attributes;
using SmartFormat;
using SmartFormat.Core.Parsing;
using StrideSaber.Diagnostics;

namespace StrideSaber.Benchmarks
{
	//Apparently string interpolation is faster lol
	/*
	 * 
	 */
	[SimpleJob, MemoryDiagnoser]
	public sealed class BackgroundTaskToStringBenchmarks
	{
		private BackgroundTask task = null!;

		[GlobalSetup]
		public void Setup()
		{
			task = new BackgroundTask("Test task with a long name for benchmarking", _ => System.Threading.Tasks.Task.CompletedTask);
			namedToString = Smart.Default.Parser.ParseFormat("BackgroundTask \"{Name}\" Id {Id} ({Progress:p0})");
			positionalToString = Smart.Default.Parser.ParseFormat("BackgroundTask \"{0}\" Id {1} ({2:p0})");
		}

		private Format namedToString = null!;
		private Format positionalToString = null!;

		[Benchmark]
		public string StringInterpolation()
		{
			return $"BackgroundTask \"{task.Name}\" Id {task.Id} ({task.Progress:p0})";
		}

		[Benchmark]
		public string SmartFormatPreParsedNamed()
		{
			return Smart.Default.Format(namedToString, task);
		}
		[Benchmark]
		public string SmartFormatPreParsedPositional()
		{
			return Smart.Default.Format(positionalToString, task.Name, task.Id, task.Progress);
		}

		[Benchmark]
		public string SmartFormat()
		{
			return Smart.Format("BackgroundTask \"{Name}\" Id {Id} ({Progress:p0})", task);
		}
	}
}