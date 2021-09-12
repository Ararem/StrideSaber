using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace StrideSaber.Modding
{
	/// <summary>
	/// A class that encapsulates functions for managing <see cref="Assembly">Assemblies</see>
	/// </summary>
	public static class AssemblyManager
	{
		/// <summary>
		/// Gets a <see cref="IList{T}"/> of all the 'external' assemblies (those that are above the inheritance level of this project)
		/// </summary>
		/// <remarks>
		///	For example, this might return the StrideSaber project and any currently loaded mods, but not any System libraries
		/// </remarks>
		public static IList<Assembly> GetAllExternalAssemblies()
		{
			//TODO: Actually make this scan or something
			return new []{
				typeof(AssemblyManager).Assembly
			};
		}
	}
}