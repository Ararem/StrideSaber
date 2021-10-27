using BenchmarkDotNet.Running;
using CommandLine;
using CommandLine.Text;
using JetBrains.Annotations;
using Serilog;
using SmartFormat;
#if DEBUG
#endif
using Stride.Core.Diagnostics;
using Stride.Engine;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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

		public static OptionsBase CmdOptions { get; private set; } = null!;

		/// <summary>
		///  Read class description lol
		/// </summary>
		/// <param name="args"></param>
		internal static void Main(IEnumerable<string> args)
		{
			//Most important things first
			ConsoleLogListener.ShowConsole();

			//First thing to do is parse the options
			OptionsBase? cmdOptions = ParseCommandLine(args);
			//Yes I know i'm possibly passing in null here, but that's fine because the null should never be propagated
			//Because it's only even null when an error happens
			//And if an error happens we don't run the game
			CmdOptions = cmdOptions!;
			//Rename the main thread
			Thread.CurrentThread.Name = "Main Thread";

			try
			{
				if (cmdOptions is DebugOptions { RunTestCommand: true }) TestCommand();
				HandleRunMode(cmdOptions);
			}
			catch (Exception e)
			{
				Log.Fatal(e, "Fatal Exception thrown");
			}
			finally
			{
				//Do cleanup
				CleanupAndExit();
			}
		}

		private static void TestCommand()
		{
			Console.WriteLine("Test");
			Console.ReadKey();
		}

		private static void HandleRunMode(OptionsBase? optionsBase)
		{
			switch (optionsBase)
			{
				case DefaultOptions options:
				{
					//Break or launch the debugger if running debug verb
					if (options is DebugOptions)
					{
						if (Debugger.IsAttached)
							Debugger.Break();
						else
							Debugger.Launch();
					}

					//Wait to initialize the logger because we don't need to bother with Bench.Net
					Logger.Init();
					EventManager.Init();

					using (Game game = CurrentGame = new Game())
					{
						//Apply the commandline settings
						if (options.MaxFps is { } maxFps)
							game.WindowMinimumUpdateRate.SetMaxFrequency(maxFps);
						else if (options.MinimumFrameTime is { } minimumFrameTime)
							game.WindowMinimumUpdateRate.MinimumElapsedTime = minimumFrameTime;
						game.GraphicsDeviceManager.SynchronizeWithVerticalRetrace = options.VSync;

						EventManager.FireEventSafeLogged(new GameLoadEvent(CurrentGame));

						//By the way, even though this isn't in the docs, the sender is the `Game` instance, and eventArgs will always be null
						Game.GameStarted += (sender, _) => EventManager.FireEventSafeLogged(new GameStartedEvent((Game)sender!));
						//Now we run the game
						game.Run();
					}

					break;
				}
				case BenchmarkDotNetOptions benchOpt:
					BenchmarkSwitcher.FromAssembly(typeof(StrideSaberApp).Assembly).Run(benchOpt.Options?.ToArray() ?? Array.Empty<string>());
					break;
				//Something messed up, dont do anything and return
				case null:
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(optionsBase), optionsBase, $"Invalid option type {optionsBase.GetType()}");
			}
		}

		/// <summary>
		///  Cleans up after the game has finished
		/// </summary>
		private static void CleanupAndExit()
		{
			CurrentGame = null!;
			Logger.Shutdown();
			Console.ResetColor();
			Console.WriteLine();
			Console.WriteLine("Game exited. Press enter to close console");
			//Loop until we get an enter key press
			while (Console.ReadKey(true).Key != ConsoleKey.Enter)
			{
			}
		}

		//TODO: Fix cleanup and exception handling
		private static OptionsBase? ParseCommandLine(IEnumerable<string> args)
		{
			//By the way, the logging system doesn't exist as of yet so we can't write to it
			//So use the console instead

			//Set up some settings for parsing
			Parser parser = new(s =>
			{
				s.HelpWriter = null;
				s.CaseInsensitiveEnumValues = true;
				s.CaseSensitive = false;
				s.AutoVersion = true;
				s.AutoHelp = true;
			});

			//Parse the options and errors
			var result = parser.ParseArguments<DefaultOptions, DebugOptions, BenchmarkDotNetOptions>(args);
			switch (result)
			{
				case Parsed<object> parsed:
					OptionsBase options = (OptionsBase)parsed.Value;
					#if DEBUG || true
					Console.Out.WriteLineSmart("Successfully got command-line options: {0}", options);
					#endif
					return options;
				case NotParsed<object> notParsed:
					var helpText = HelpText.AutoBuild(notParsed);
					Error[] errors = notParsed.Errors.ToArray();
					//If the *only* error is a help error
					// ReSharper disable once ConvertIfStatementToSwitchStatement
					if ((errors.Length == 1) && errors[0] is HelpRequestedError or HelpVerbRequestedError or UnknownOptionError { Token: "help" })
					{
						Console.WriteLine("Displaying help:");
						Console.WriteLine(helpText);
					}
					//Same for version
					else if ((errors.Length == 1) && errors[0] is VersionRequestedError or UnknownOptionError { Token: "version" })
					{
						Console.WriteLine("Displaying version:");
						Console.WriteLine(helpText);
					}
					else
					{
						Console.ForegroundColor = ConsoleColor.Red;
						Console.WriteLine("Error parsing command-line options:");
						Console.WriteLine(helpText);
					}

					return null;
				default:
					Console.ForegroundColor = ConsoleColor.Red;
					Console.WriteLine("Command-line parser failed to return a result");
					return null;
			}
		}
	}
}