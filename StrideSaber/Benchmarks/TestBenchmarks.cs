using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using LibEternal.ObjectPools;
using StrideSaber.Diagnostics;

//ReSharper disable all
namespace StrideSaber.Benchmarks
{
	/// <summary>
	/// Test Benchmarks
	/// </summary>
	[SimpleJob, MemoryDiagnoser, PublicAPI, BaselineColumn]
	// ReSharper disable once ClassCanBeSealed.Global
	#pragma warning disable CA1822
	public class TestBenchmarks
	{
		private TrackedTask task;

		/// <summary>
		/// Sets up stuff
		/// </summary>
		[GlobalSetup]
		public void Setup()
		{
			task = new TrackedTask("Name", async _ =>
			{
				while (true) ;
			});
		}

		/// <summary>
		/// Runs the actual benchmark code
		/// </summary>
		[Benchmark(Baseline = true)]
		public string Interpolation()
		{
			return $"{task.Name}:\t{task.Progress,3:p0}";
		}

		/// <summary>
		/// Runs the actual benchmark code
		/// </summary>
		[Benchmark]
		public string Builder()
		{
			return StringBuilderPool.BorrowInline(static (sb, task) => sb.Append(task.Name).Append(":\t").Append(task.Progress.ToString(",3:p0")), task);
		}
	}
}