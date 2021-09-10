using Serilog;
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
		private static void TestGameStartedEvent()
		{
			Log.Information("Game started event called");
		}
		[EventMethod(typeof(GameLoadEvent))]
		private static void TestGameLoadEvent()
		{
			Log.Information("Game load event called");
		}

		[EventMethod(typeof(GameStartedEvent))]
		[EventMethod(typeof(GameLoadEvent))]
		private static void GameStartOrLoadEvent()
		{
			Log.Information("Game started/Game load event called");
		}
		//(sender, _) => (sender as Game)!.Window.AllowUserResizing = true
	}
}