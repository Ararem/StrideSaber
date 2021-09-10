using System;
using System.Collections.Generic;
using System.Reflection;

namespace StrideSaber.Modding
{
	public static class AssemblyManager
	{
		public static IList<Assembly> GetAllAssemblies()
		{
			//TODO: Actually make this scan or something
			return AppDomain.CurrentDomain.GetAssemblies();
			// return new []{
			// 	typeof(AssemblyManager).Assembly
			// };
		}
	}
}