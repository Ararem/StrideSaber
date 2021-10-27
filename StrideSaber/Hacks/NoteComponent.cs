using JetBrains.Annotations;
using Stride.Engine;

namespace StrideSaber.Hacks
{
	/// <summary>
	/// A component that can be used to attach a note to an entity
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public sealed class NoteComponent : ScriptComponent
	{
		/// <summary>
		/// The note for the entity
		/// </summary>
		public string? Note = null;
	}
}