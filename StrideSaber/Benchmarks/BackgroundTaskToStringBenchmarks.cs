﻿using BenchmarkDotNet.Attributes;
using SmartFormat;
using SmartFormat.Core.Parsing;
using StrideSaber.Diagnostics;

namespace StrideSaber.Benchmarks
{
	//Apparently string interpolation is faster lol
	//And with less memory :(
	[SimpleJob, MemoryDiagnoser]
	// ReSharper disable once ClassCanBeSealed.Global
	public class BackgroundTaskToStringBenchmarks
	{
		private TrackedTask task = null!;

		[GlobalSetup]
		public void Setup()
		{
			task = new TrackedTask("Test task with a long name for benchmarking", _ => System.Threading.Tasks.Task.CompletedTask);
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