using Microsoft.Extensions.ObjectPool;
using OpenTK;
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
	/// Test XML docs lol
	/// </summary>
	/// <inheritdoc cref=""/>
	[GenerateStaticInstanceMembers("StrideSaber.Testing.Generated", "Static", "Instance")]
	public class StaticInstGenClass
	{
		// public int IntProperty { get; set; }
		// public int IntField;
		// public Random RandomField;
		// public void OrdinaryVoidMethod(){}
		
		// public async void AsyncVoidMethod(){}
		// public async Task AsyncTaskMethod(){}

		public T Asd<T>(T t) where T : class, IEnumerable, new() => new T();
		public T1 Asd<T1, T2>(T2 t2) where T1 : class, IEnumerable<T1>, new() where T2 : unmanaged, IEquatable<IEnumerable<Dictionary<int, string>>> => new T1();
	}
}