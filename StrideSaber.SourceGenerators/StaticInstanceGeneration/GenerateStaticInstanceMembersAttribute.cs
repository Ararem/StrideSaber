using JetBrains.Annotations;
using System;

namespace StrideSaber.SourceGenerators.StaticInstanceGeneration
{
	/// <summary>
	/// Marks a class or struct so that any instance members (properties, fields or methods) will also be duplicated into static members, which will call their counterparts on the targeted instance
	/// </summary>
	/// <remarks>The generated class will be partial, so you can add custom code yourself if needed</remarks>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	[PublicAPI]
	public class GenerateStaticInstanceMembersAttribute : Attribute
	{
		/// <summary>
		/// The namespace the generated type will be placed in.
		/// </summary>
		public readonly string GeneratedTypeNamespace;

		/// <summary>
		/// The name of the type that should be generated
		/// </summary>
		public readonly string GeneratedTypeName;

		/// <summary>
		/// The name of the variable that will be used as the instance
		/// </summary>
		public readonly string GeneratedInstanceName;

		/// <summary>The constructor for a <see cref="GenerateStaticInstanceMembersAttribute"/></summary>
		/// <param name="generatedTypeNamespace">The namespace the generated type will be placed in</param>
		/// <param name="generatedTypeName" >The name of the type that should be generated</param>
		///  <param name="generatedInstanceName">The name of the variable that will be used as the instance</param>
		public GenerateStaticInstanceMembersAttribute(string generatedTypeNamespace, string generatedTypeName, string generatedInstanceName)
		{
			GeneratedTypeName = generatedTypeName;
			GeneratedInstanceName = generatedInstanceName;
			GeneratedTypeNamespace = generatedTypeNamespace;
		}
	}
}