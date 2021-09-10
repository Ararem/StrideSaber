using System;
using System.Collections.Generic;
using System.Reflection;

namespace StrideSaber.Modding
{
	/// <summary>
	/// A class that encapsulates functions for managing <see cref="Assembly">Assemblies</see>
	/// </summary>
	public static class AssemblyManager
	{
		public static IList<Assembly> GetAllExternalAssemblies()
		{
			//TODO: Actually make this scan or something
			return AppDomain.CurrentDomain.GetAssemblies();
			// return new []{
			// 	typeof(AssemblyManager).Assembly
			// };
		}
	}
}