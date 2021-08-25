using JetBrains.Annotations;
using StrideSaber.SourceGenerators;
using StrideSaber.SourceGenerators.StaticInstanceGeneration;

namespace StrideSaber.Testing
{
	[GenerateStaticInstanceMembers("Static")]
	public partial class StaticInstGenClass
	{
		public int IntProperty { get; set; }
		public int IntField;
		public void Method(){}
	}
}