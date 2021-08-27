using StrideSaber.SourceGenerators.StaticInstanceGeneration;
using System;
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
		public void OrdinaryVoidMethod(){}
		
		public async void AsyncVoidMethod(){}
		public async Task AsyncTaskMethod(){}

		// public T1 Asd<T1, T2>() where T1 : class, new() where T2 : notnull => new T1();
		//
		// public T Test<T>(T t)
		// {
		// 	return t;
		// }
	}
}