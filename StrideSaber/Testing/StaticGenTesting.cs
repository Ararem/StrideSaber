using StrideSaber.SourceGenerators.StaticInstanceGeneration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

#pragma warning disable

namespace StrideSaber.Testing
{
	//ReSharper disable all
	/// <summary>
	///  Test XML docs lol
	/// </summary>
	[GenerateStaticInstanceMembers("StrideSaber.Testing.Generated", "Static", "Instance")]
	public class StaticInstGenClass
	{
	#region Simple stuff

		/// <summary>
		///  Get set int propert
		/// </summary>
		public int GetSetIntProperty { get; set; }

		/// <summary>
		///  Just a simple int field
		/// </summary>
		public int IntField;

		/// <summary>
		///  A <see cref="System.Random"/> field
		/// </summary>
		public System.Random SystemRandomField;

		public void VoidMethod()
		{
		}

		public string ReturnsAString() => "";
		public double F_Of_X(int a, float b, double c, int x) => a * x * x + b * x + c;
		public bool ToBeOrNotToBe() => true;

		public Task NotAnAsyncTask() => Task.CompletedTask;

	#endregion

	#region Async

		public async void AsyncVoid()
		{
		}

		public async Task AsyncTask()
		{
		}

		public async Task<T> GenericAsyncTask<T>(T t)
		{
			return t;
		}

		public async Task GenericAsyncWithConstraints<T>() where T : class, IEnumerable<T>, new()
		{
			await Task.CompletedTask;
		}

	#endregion


	#region Generic Tests

		public T Simple<T>(T t) => t;
		public T OneComplexParameter<T>(T t) where T : class, IEnumerable, new() => new T();
		public T1 ComplexParams<T1, T2>(T2 t2)
				where T1 : class, IEnumerable<T1>, new() where T2 : unmanaged, IEquatable<IEnumerable<Dictionary<int, string>>> 
			=> new T1();

		public TBase VeryComplexInheritance<TBase, TInherited>(TBase @base, TInherited inherited) where TInherited : class, TBase, IEnumerable<TBase>, IComparable<TInherited>, IEnumerable
				where TBase : notnull, Delegate
		{
			return @base;
		}

	#endregion
	}
}