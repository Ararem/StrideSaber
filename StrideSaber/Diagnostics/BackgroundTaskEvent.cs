using System;

namespace StrideSaber.Diagnostics
{
	[Flags]
	public enum BackgroundTaskEvent
	{
		None = 0,
		Created = 1,
		Disposed = 2,
		Error = 4,
		Success = 8,
		ProgressUpdated = 16,
	}
}