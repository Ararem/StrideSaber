using JetBrains.Annotations;
using Serilog;
using Stride.Core.Diagnostics;
using Stride.Engine;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using System;
using System.Threading;
using Logger = StrideSaber.Logging.Logger;

namespace StrideSaber.Startup
{
	/// <summary>
	///  Literally just the main function
	/// </summary>
	internal static class StrideSaberApp
	{
		/// <summary>
		///  The currently running <see cref="Game"/>
		/// </summary>
		/// <remarks>
		///  Should literally never be null (initialized before the game is even run, in the <c>Main()</c> function)
		/// </remarks>
		[PublicAPI]
		public static Game CurrentGame { get; private set; } = null!;

		/// <summary>
		///  Read class description lol
		/// </summary>
		/// <param name="args"></param>
		// ReSharper disable once UnusedParameter.Global
		internal static void Main(string[] args)
		{
			try
			{
				//Most important things first
				ConsoleLogListener.ShowConsole();
				Stride.Core.Diagnostics.Logger.MinimumLevelEnabled = LogMessageType.Verbose;

				//Rename the main thread
				Thread.CurrentThread.Name = "Main Thread";
				Logger.Init();

				EventManager.Init();

				using Game game = CurrentGame = new Game();
				game.WindowMinimumUpdateRate.SetMaxFrequency(30 /*fps*/); //Throttle the
				//Set up an unhanded exception handler
				game.UnhandledException += (s, e) => OnUnhandledException(s, (Exception) e.ExceptionObject);
				EventManager.FireEventSafeLogged(new GameLoadEvent(CurrentGame));

				//By the way, even though this isn't in the docs, the sender is the `Game` instance, and eventArgs will always be null
				Game.GameStarted += (sender, _) => EventManager.FireEventSafeLogged(new GameStartedEvent((Game) sender!));
				//Now we run the game
				game.Run();
			}
			//Don't need to do this because our `game.UnhandledException` catches it anyway
			// catch (Exception e)
			// {
			// 	OnUnhandledException(null, e, true);
			// }
			finally
			{
				//Do cleanup
				Cleanup();
				Console.WriteLine("Game exited. Press enter to close console");
				//Loop until we get an enter key press
				while (Console.ReadKey(true).Key != ConsoleKey.Enter)
				{
				}
			}
		}

		/// <summary>
		///  Cleans up after the game has finished
		/// </summary>
		private static void Cleanup()
		{
			//Only thing to do is close the logger
			Logger.Shutdown();
		}

		/// <summary>
		///  Called whenever there is an unhandled exception
		/// </summary>
		private static void OnUnhandledException(object? sender, Exception e)
		{
			ConsoleLogListener.ShowConsole();
			//Log the exception
			Log.ForContext("Sender", sender)
			   .Fatal(e, "Unhandled Exception");
			Cleanup();

			// SDL.SDL_MessageBoxData data = new()
			// {
			// 		flags = SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR,
			// 		title = "Unhandled Exception",
			// 		message = $"Unhandled Exception Caught:\n{e.ToStringDemystified()}",
			// 		buttons = new[]
			// 		{
			// 				new SDL.SDL_MessageBoxButtonData {text = "Ok", buttonid = 0, flags = SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_ESCAPEKEY_DEFAULT | SDL.SDL_MessageBoxButtonFlags.SDL_MESSAGEBOX_BUTTON_RETURNKEY_DEFAULT}
			// 		},
			// 		numbuttons = 1,
			// 		colorScheme = new SDL.SDL_MessageBoxColorScheme
			// 		{
			// 				colors = new[]
			// 				{
			// 						// .colors (.r, .g, .b)
			// 						// [SDL_MESSAGEBOX_COLOR_BACKGROUND]
			// 						new SDL.SDL_MessageBoxColor {r = 255, g = 0, b = 0},
			// 						// [SDL_MESSAGEBOX_COLOR_TEXT]
			// 						new SDL.SDL_MessageBoxColor {r = 0, g = 255, b = 0},
			// 						// [SDL_MESSAGEBOX_COLOR_BUTTON_BORDER]
			// 						new SDL.SDL_MessageBoxColor {r = 255, g = 255, b = 0},
			// 						// [SDL_MESSAGEBOX_COLOR_BUTTON_BACKGROUND]
			// 						new SDL.SDL_MessageBoxColor {r = 0, g = 0, b = 255},
			// 						// [SDL_MESSAGEBOX_COLOR_BUTTON_SELECTED]
			// 						new SDL.SDL_MessageBoxColor {r = 255, g = 0, b = 255},
			// 				}
			// 		}
			// };
			// SDL.SDL_ShowMessageBox(ref data, out int _);
		}
	}
}