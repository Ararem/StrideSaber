using System;

namespace StrideSaber.Core.SourceGenerators
{
	/// <summary>
	/// Marks a (partial) class or struct so that any instance members (properties, fields or methods) will also be duplicated into static members, which will call their counterparts on the targeted instance
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public class GenerateStaticInstanceMembersAttribute : Attribute
	{
	}
}