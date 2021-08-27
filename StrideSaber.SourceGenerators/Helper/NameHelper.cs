using Microsoft.CodeAnalysis;

namespace StrideSaber.SourceGenerators.Helper
{
	public static class NameHelper
	{
		/// <summary>
		/// Returns the typed name of a given parameter (E.g. <c>System.Int32 intParameter</c>)
		/// </summary>
		/// <param name="obj">The property to return the type and name of</param>
		public static string TypeAndName(IParameterSymbol obj)
		{
			return $"{obj.Type} {obj.Name}";
		}
		/// <summary>
		/// Returns the typed name of a given property (E.g. <c>System.Int32 IntProperty</c>)
		/// </summary>
		/// <param name="obj">The property to return the type and name of</param>
		public static string TypeAndName(IPropertySymbol obj)
		{
			return $"{obj.Type} {obj.Name}";
		}

		/// <summary>
		/// Returns the typed name of a given field (E.g. <c>System.Int32 IntField</c>)
		/// </summary>
		/// <param name="obj">The field to return the type and name of</param>
		public static string TypeAndName(IFieldSymbol obj)
		{
			return $"{obj.Type} {obj.Name}";
		}
	}
}