using JetBrains.Annotations;
using System;

namespace StrideSaber.SourceGenerators.StaticInstanceGeneration
{
	/// <summary>
	/// Marks a class or struct so that any instance members (properties, fields or methods) will also be duplicated into static members, which will call their counterparts on the targeted instance
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	[PublicAPI]
	public class GenerateStaticInstanceMembersAttribute : Attribute
	{
		/// <summary>
		/// The name of the type that should be generated
		/// </summary>
		public string GeneratedTypeName { get; init; }

		public GenerateStaticInstanceMembersAttribute(string generatedTypeName)
		{
			GeneratedTypeName = generatedTypeName;
		}
	}
}