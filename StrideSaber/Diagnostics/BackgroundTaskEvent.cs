using System;

namespace StrideSaber.Diagnostics
{
	/// <summary>
	/// An enum that specifies events that can happen in the lifetime of a <see cref="BackgroundTask"/>
	/// </summary>
	[Flags]
	public enum BackgroundTaskEvent
	{
		/// <summary>
		/// A flag that is used to signify that no events should be logged
		/// </summary>
		/// <seealso cref="BackgroundTask.EnabledLogEvents"/>
		None = 0,

		/// <summary>
		/// A <see cref="BackgroundTask"/> was created by calling <c>new()</c>
		/// </summary>
		Created = 1,
		/// <summary>
		/// A call to <see cref="BackgroundTask.Dispose"/> was made
		/// </summary>
		Disposed = 2,
		/// <summary>
		/// An exception was thrown during execution of the <see cref="BackgroundTaskDelegate"/>
		/// </summary>
		Error = 4,
		/// <summary>
		/// The task completed successfully
		/// </summary>
		Success = 8,
		/// <summary>
		/// The <see cref="BackgroundTask.Progress"/> of the task was updated
		/// </summary>
		ProgressUpdated = 16,
	}
}