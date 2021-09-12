using Stride.Engine;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;

namespace StrideSaber.Startup
{
	/// <summary>
	/// Handles events that occur when the game is started
	/// </summary>
	internal static class StartupEventHandler
	{
		[EventMethod(typeof(GameStartedEvent))]
		private static void AllowWindowResizing(Event e)
		{
			Game g = ((GameStartedEvent) e).Game;
			g.Window.AllowUserResizing = true;
		}
	}
}