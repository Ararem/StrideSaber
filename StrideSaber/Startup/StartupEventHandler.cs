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
		// [EventMethod(typeof(GameStartedEvent))]
		// private static void TestGameStartedEvent(Event @event)
		// {
		// 	Log.Information("Game started event called: {Event}", @event);
		// }
		// [EventMethod(typeof(GameLoadEvent))]
		// private static void TestGameLoadEvent(Event @event)
		// {
		// 	Log.Information("Game load event called: {Event}", @event);
		// }
		//
		// [EventMethod(typeof(GameStartedEvent))]
		// [EventMethod(typeof(GameLoadEvent))]
		// private static void GameStartOrLoadEvent(Event @event)
		// {
		// 	Log.Information("Game started/Game load event called: {Event}", @event);
		// }

		[EventMethod(typeof(GameLoadEvent))]
		private static string Test_ReturnsNonVoid()
		{
			return "yeet";
		}
		//(sender, _) => (sender as Game)!.Window.AllowUserResizing = true
	}
}