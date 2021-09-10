using StrideSaber.Events;

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
			
		}
		[EventMethod(typeof(GameLoadEvent))]
		private static void TestGameLoadEvent()
		{
			
		}
	}
}