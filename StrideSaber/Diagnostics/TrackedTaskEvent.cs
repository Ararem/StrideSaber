using System;

namespace StrideSaber.Diagnostics
{
	/// <summary>
	/// An enum that specifies events that can happen in the lifetime of a <see cref="TrackedTask"/>
	/// </summary>
	[Flags]
	public enum TrackedTaskEvent
	{
		/// <summary>
		/// A flag that is used to signify that no events should be logged
		/// </summary>
		/// <seealso cref="TrackedTask.EnabledLogEvents"/>
		None = 0,

		/// <summary>
		/// A <see cref="TrackedTask"/> was created by calling <c>new()</c>
		/// </summary>
		Created = 1,
		/// <summary>
		/// A call to <see cref="TrackedTask.Dispose"/> was made
		/// </summary>
		Disposed = 2,
		/// <summary>
		/// An exception was thrown during execution of the <see cref="TrackedTaskDelegate"/>
		/// </summary>
		Error = 4,
		/// <summary>
		/// The task completed successfully
		/// </summary>
		Success = 8,
		/// <summary>
		/// The <see cref="TrackedTask.Progress"/> of the task was updated
		/// </summary>
		ProgressUpdated = 16,
	}
}