using Serilog;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;
using Stride.Games;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;

namespace StrideSaber.Startup
{
	/// <summary>
	///  Handles hacks that need to wait until after the <see cref="Game.GameStarted">game is started</see>
	/// </summary>
	internal static class GameStartedEventHacks
	{
		/// <summary>
		/// Disables the splash screen if the option was specified
		/// </summary>
		/// <seealso cref="DefaultOptions.NoSplash"/>
		/// <seealso cref="GameSettings.SplashScreenUrl"/>
		/// <seealso cref="SceneSystem.SplashScreenEnabled"/>
		/// <seealso cref="SceneSystem.SplashScreenUrl"/>
		[EventMethod(typeof(GameStartedEvent))]
		private static void MaybeDisableSplash(Event e)
		{
			//Have to do this here because the bloody thing resets it when the game is started
			if(StrideSaberApp.CmdOptions is DefaultOptions { NoSplash: true})
			{
				Log.Debug("Disabling splash screen");
				StrideSaberApp.CurrentGame.Settings.SplashScreenUrl = null;
				StrideSaberApp.CurrentGame.SceneSystem.SplashScreenEnabled = false;
				StrideSaberApp.CurrentGame.SceneSystem.SplashScreenUrl = null;
			}
		}

		/// <summary>
		/// Enables resizing of the game window, and sets the size and position of it
		/// </summary>
		/// <seealso cref="GameWindow.SetSize"/>
		/// <seealso cref="GameWindow.Position"/>
		/// <seealso cref="GameWindow.AllowUserResizing"/>
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

			Log.Debug("Window Bounds: {WindowBounds}", window.ClientBounds);
		}
	}
}