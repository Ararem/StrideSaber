using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using LibEternal.ObjectPools;
using SmartFormat;
using SmartFormat.Core.Output;
using StrideSaber.Diagnostics;
using System.Text;
using Format = SmartFormat.Core.Parsing.Format;

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
		private Format      cachedFormatIndexed;
		private Format      cachedFormatReflect;

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
			cachedFormatIndexed = Smart.Default.Parser.ParseFormat("{0}:\t{1,3:p0}");
			cachedFormatReflect = Smart.Default.Parser.ParseFormat("{Name}:\t{Progress,3:p0}");
		}

		/// <summary>
		/// String interpolation
		/// </summary>
		[Benchmark(Baseline = true)]
		public string Interpolation()
		{
			return $"{task.Name}:\t{task.Progress,3:p0}";
		}

		/// <summary>
		/// StringBuilder.Append, with the progress being ToString()'ed before passed into append
		/// </summary>
		[Benchmark]
		public string Append_ToString_Format()
		{
			return StringBuilderPool.BorrowInline(static (sb, task) => sb
																		.Append(task.Name)
																		.Append(":\t")
																		.Append(task.Progress.ToString(",3:p0"))
					, task);
		}

		/// <summary>
		/// No preformatting, uses AppendFOrmat
		/// </summary>
		[Benchmark]
		public string AppendFormat()
		{
			return StringBuilderPool.BorrowInline(static (sb, task) => sb
																		.Append(task.Name)
																		.Append(":\t")
																		.AppendFormat("{0,3:p0}", task.Progress)
					, task);
		}

		/// <summary>
		/// SmartFormat with indexed format
		/// </summary>
		[Benchmark]
		public string CachedSmartIndexed()
		{
			return Smart.Default.Format(cachedFormatIndexed, task.Name, task.Progress);
		}

		/// <summary>
		/// Smart format with reflection
		/// </summary>
		[Benchmark]
		public string CachedSmartReflect()
		{
			return Smart.Default.Format(cachedFormatReflect, task);
		}

		/// <summary>
		/// Uses a cached builder and cached index format
		/// </summary>
		[Benchmark]
		public string CachedBuilderSmartIndexed()
		{
			var     sb     = StringBuilderPool.GetPooled();
			IOutput output = new StringOutput(sb);
			Smart.Default.FormatInto(output, cachedFormatIndexed, task.Name, task.Progress);
			return StringBuilderPool.ReturnToString(sb);
		}

		/// <summary>
		/// Uses a cached builder and cached reflection format
		/// </summary>
		[Benchmark]
		public string CachedBuilderSmartReflect()
		{
			var     sb     = StringBuilderPool.GetPooled();
			IOutput output = new StringOutput(sb);
			Smart.Default.FormatInto(output, cachedFormatReflect, task);
			return StringBuilderPool.ReturnToString(sb);
		}
	}
}