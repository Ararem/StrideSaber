using Serilog;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;

namespace StrideSaber.Startup
{
	/// <summary>
	///  Handles events that occur when the game is started
	/// </summary>
	internal static class StartupEventHandler
	{
		[EventMethod(typeof(GameStartedEvent))]
		private static void SetWindowProperties(Event e)
		{
			Log.Debug("Enabling resizing of window");
			Game game = ((GameStartedEvent) e).Game;
			GameWindow window = game.Window;
			window.AllowUserResizing = true;

			Log.Debug("Setting size and position of window");
			window.Position = Int2.Zero;
			window.SetSize(new Int2(1280, 720));
			Log.Error("{X}", window.ClientBounds);
		}
	}
}