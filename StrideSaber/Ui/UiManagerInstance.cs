using LibEternal.SourceGenerators.StaticInstanceGeneration;
using Stride.Engine;

namespace StrideSaber.Ui
{
	[GenerateStaticInstanceMembers("StrideSaber.Ui", "UiManager", "instance")]
	public sealed class UiManagerInstance
	{
		/// <summary>
		/// The library containing all the base elements, such as buttons, text blocks, etc
		/// </summary>
		public UILibrary BaseElementLibrary = null!;
	}
}