using Serilog;
using Stride.Core.Diagnostics;
using Stride.Engine;
using System;
using System.Reflection;
using System.Threading;
using Logger = TestGame.Core.Logging.Logger;

namespace TestGame.Core.Startup
{
	/// <summary>
	/// Literally just the main function
	/// </summary>
	internal static class Program
	{
		private static void Test()
		{
			// Log.Fatal("Fatal");
			// Log.Error("Error");
			// Log.Warning("Warning");
			// Log.Information("Information");
			// Log.Debug("Debug");
			// Log.Verbose("Verbose");
			// Console.WriteLine("Console Direct");
			// GlobalLogger.GetLogger("Program").Info("GlobalLogger", CallerInfo.Get());
		}

		/// <summary>
		/// Read class description lol
		/// </summary>
		internal static void Main()
		{
			try
			{
				//Most important things first
				ConsoleLogListener.ShowConsole();

				//Here I'm clearing the event because stride sets up it's own handler which I don't want
				//(Otherwise you would get duped logs when debugging which is annoying)
				//This is because by default stride logs to the console, but I'm doing that myself
				typeof(GlobalLogger).GetField(nameof(GlobalLogger.GlobalMessageLogged), BindingFlags.Static | BindingFlags.NonPublic)!.SetValue(null, null);
				Logger.Init();

				//Set up some game variables
				//I can't call game.Window before the game is started because it's null, but I can't call it after because it blocks, so do it with an event
				Game.GameStarted += (sender, _) => (sender as Game)!.Window.AllowUserResizing = true;
				//Rename the main thread
				Thread.CurrentThread.Name = "Main Thread";

				using Game game = new();
				//Set up an unhanded exception handler
				game.UnhandledException += (s, e) => OnUnhandledException(s, (Exception) e.ExceptionObject, e.IsTerminating);
				AppDomain.CurrentDomain.UnhandledException += (s, e) => OnUnhandledException(s, (Exception) e.ExceptionObject, e.IsTerminating);

				Test();

				//Now we run the game
				game.Run();
			}
			catch (Exception e)
			{
				OnUnhandledException(null, e, true);
			}
			finally
			{
				//Do cleanup
				Cleanup();
			}
		}

		private static void Cleanup()
		{
			//Only thing to do is close the logger
			Logger.Shutdown();
		}

		/// <summary>
		/// Called whenever there is an unhandled exception
		/// </summary>
		private static void OnUnhandledException(object? sender, Exception e, bool terminating)
		{
			ConsoleLogListener.ShowConsole();
			//Log the exception
			Log.ForContext("Sender", sender)
					.Fatal(e, "AppDomain Unhandled Exception (Terminating = {IsTerminating})", terminating);
			//If the CLR's going to terminate, make sure to cleanup (not sure if Main() finally gets called)
			if (terminating)
				Cleanup();

			Console.ResetColor();
			Console.WriteLine("Press any key to exit");
			Console.ReadKey(true);

			//This doesn't work for some reason :(
			// SDL.SDL_MessageBoxData data = new()
			// {
			// 		flags = terminating ? SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_ERROR : SDL.SDL_MessageBoxFlags.SDL_MESSAGEBOX_WARNING,
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
			// 						/* .colors (.r, .g, .b) */
			// 						/* [SDL_MESSAGEBOX_COLOR_BACKGROUND] */
			// 						new SDL.SDL_MessageBoxColor {r = 255, g = 0, b = 0},
			// 						/* [SDL_MESSAGEBOX_COLOR_TEXT] */
			// 						new SDL.SDL_MessageBoxColor {r = 0, g = 255, b = 0},
			// 						/* [SDL_MESSAGEBOX_COLOR_BUTTON_BORDER] */
			// 						new SDL.SDL_MessageBoxColor {r = 255, g = 255, b = 0},
			// 						/* [SDL_MESSAGEBOX_COLOR_BUTTON_BACKGROUND] */
			// 						new SDL.SDL_MessageBoxColor {r = 0, g = 0, b = 255},
			// 						/* [SDL_MESSAGEBOX_COLOR_BUTTON_SELECTED] */
			// 						new SDL.SDL_MessageBoxColor {r = 255, g = 0, b = 255},
			// 				}
			// 		}
			// };
			// SDL.SDL_ShowMessageBox(ref data, out int _);
		}
	}
}