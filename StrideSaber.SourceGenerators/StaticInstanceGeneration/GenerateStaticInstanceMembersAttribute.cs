using JetBrains.Annotations;
using System;

namespace StrideSaber.SourceGenerators.StaticInstanceGeneration
{
	/// <summary>
	/// Marks a (partial) class or struct so that any instance members (properties, fields or methods) will also be duplicated into static members, which will call their counterparts on the targeted instance
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	[PublicAPI]
	public class GenerateStaticInstanceMembersAttribute : Attribute
	{
	}
}