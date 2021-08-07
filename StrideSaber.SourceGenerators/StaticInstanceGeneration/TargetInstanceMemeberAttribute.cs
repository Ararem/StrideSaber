using System;

namespace StrideSaber.SourceGenerators
{
	/// <inheritdoc />
	/// <summary>
	/// Marks a field or property as the target member to be used for static instance members
	/// </summary>
	/// <remarks>
	/// Essentially makes all static versions of instance members be called on this object instead</remarks>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
	public sealed class TargetInstanceMemberAttribute : Attribute
	{
	}
}