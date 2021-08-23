using JetBrains.Annotations;
using StrideSaber.SourceGenerators;
using StrideSaber.SourceGenerators.StaticInstanceGeneration;

namespace StrideSaber.Testing
{
	//Should be fine
	[GenerateStaticInstanceMembers]
	public class Class
	{
		
	}
	[GenerateStaticInstanceMembers]
	public struct Struct
	{
		
	}
	
	[GenerateStaticInstanceMembers]
	public record Record
	{
		
	}
	
	[GenerateStaticInstanceMembers]
	public class Error_TooMany
	{
		[TargetInstanceMember] public Error_TooMany Member1;
		[TargetInstanceMember] public Error_TooMany Member2;
	}

	[GenerateStaticInstanceMembers]
	public static class Error_Static
	{
		
	}
}