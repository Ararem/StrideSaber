using JetBrains.Annotations;
using LibEternal.ObjectPools;
using Stride.Core.Mathematics;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using StrideSaber.Diagnostics;
using StrideSaber.Startup;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLog = Serilog.Log;

namespace StrideSaber.SceneSpecific.Progress_Ui
{
	/// <summary>
	/// A script that controls UI elements to display the progress of all the currently running <see cref="TrackedTask"/> instances
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class ProgressUiController : SyncScript
	{
		/// <summary>
		/// The <see cref="UIComponent"/> that this script will control
		/// </summary>
		public UIComponent Ui = null!;

		/// <summary>
		/// 
		/// </summary>
		public UrlReference<UILibrary> LibraryReference = null!;

		private UILibrary library = null!;
		private StackPanel indicatorsPanel = null!;

	#region Testing

		/// <inheritdoc />
		public override void Start()
		{
			indicatorsPanel = Ui.Page.RootElement.FindVisualChildOfType<StackPanel>("Indicators");
			library = Content.Load(LibraryReference);
			//Create some tasks for fun
			Task.Run(AsyncTaskCreator);
			SLog.Information("Task creator started");
		}

		private static Random r = new();

		//Creates tasks for debugging purposes
		[SuppressMessage("ReSharper", "All")]
		private static async Task AsyncTaskCreator()
		{
			_ = new TrackedTask("Fps", FpsTask) { Id = Guid.Empty };
			int i = 0;
			while (true)
			{
				//Wait a while between each task instantiation
				int delay = r.Next(0, 100);
				await Task.Delay(delay);
				//Ensure not too many tasks
				if (TrackedTask.UnsafeInstances.Count < 50)
					_ = new TrackedTask($"Test task {++i}", AsyncTaskTest);
			}
		}

		//A task that updates the progress from the current FPS
		[SuppressMessage("ReSharper", "All")]
		private static async Task FpsTask(Action<float> updateProgress)
		{
			while (true)
			{
				updateProgress(StrideSaberApp.CurrentGame.UpdateTime.FramePerSecond / 100);
				await Task.Delay(100);
			}
		}

		[SuppressMessage("ReSharper", "All")]
		private static async Task AsyncTaskTest(Action<float> updateProgress)
		{
			DateTime start = DateTime.Now;
			DateTime end = start + TimeSpan.FromMilliseconds(r.Next(0, 5000));
			while (DateTime.Now < end)
			{
				await Task.Delay(50);
				updateProgress((float)((DateTime.Now - start) / (end - start)));
			}

			updateProgress(1);
		}

	#endregion

		private static Color
				backgroundColour = new(69, 69, 69, 255),
				tickColour = new(211, 211, 211, 255);

		/// <summary>
		/// Updates the UI from the current tasks and their statuses
		/// </summary>
		// PERF: Hot path, needs lots of performance improvements, namely:
		// 1: Task string formatting handling
		// 2: Child finding for components - Uses LINQ
		// 3: Almost nothing is cached, this would be a massive improvement
		// 4: Colours for sliders - perhaps internally create a list of ~1000 combinations and pull out the closest approximation?
		// Texture reuse - `tex.SetData(Game.GraphicsContext.CommandList, new[]{colour});` Needs texture to have `GraphicsResourceUsage.Dynamic`
		public override void Update()
		{
			//Get all the current tasks, ordered by ID
			var tasks = TrackedTask.CloneAllInstances()
			                       .OrderBy(static t => t.Id)
			                       .ToArray();

			StringBuilder sb = StringBuilderPool.GetPooled();
			for (int i = 0; i < tasks.Length; i++)
			{
				UIElement indicator;
				TrackedTask task = tasks[i];
				//Reuse an old ui element if we have one
				if (indicatorsPanel.Children.Count > i)
				{
					indicator = indicatorsPanel.Children[i];
				}
				//Need to create a new element
				else
				{
					indicator = library.InstantiateElement<UIElement>("Progress Indicator");
					indicatorsPanel.Children.Add(indicator);
				}

				//Set the text and slider values for the task
				sb.Clear();
				//The Builder is ~50% slower than interpolation, but ~40% less memory
				sb.Append(task.Name)
				  .Append(":\t")
				  .Append(task.Progress.ToString("p0").PadLeft(3));
				indicator.FindVisualChildOfType<TextBlock>().Text = sb.ToString();
				Slider slider = indicator.FindVisualChildOfType<Slider>();
				slider.Value = task.Progress;

				//Try and give the task a nice little colour too
				//This one is based on the hash of the task ID
				Color uniqueTaskColour = Color.FromRgba(task.Id.GetHashCode());
				uniqueTaskColour.A = 255;
				//This one is based on the progression of the task
				//TODO: This is decent for now, but maybe fix the tick lines
				//Assign all the colours
				(indicator as Border)!.BorderColor = uniqueTaskColour; //Unique border for each task
				//Background and tick sprites should be consistent so store them, but unique sprites won't be because they will keep changing
				//FIXME: Need to dispose of the old value here, this is a memory leak
				slider.TrackBackgroundImage = SpriteFromColour(backgroundColour);
				slider.TickImage = SpriteFromColour(tickColour);
				slider.TrackForegroundImage = SpriteFromColour(uniqueTaskColour);
			}

			StringBuilderPool.ReturnPooled(sb);

			//Remove any extra elements we didn't use
			//So we start from our task count, and go up to our child element count
			//Task = 10
			//Children = 20
			//Start at index 10 in children (10-1=9 is last task)
			//End at children -1
			//Do this in reverse so we can use `RemoveAt()` (which is faster than `Remove()`) without any complex maths
			for (int i = indicatorsPanel.Children.Count - 1; i >= tasks.Length; i--) indicatorsPanel.Children.RemoveAt(i);
		}

		/// <summary>
		/// A dictionary cache that maps a <see cref="Color"/> to a pre-generated sprite
		/// </summary>
		private static readonly Dictionary<Color, ISpriteProvider> CachedSpriteFromColour = new();

		internal ISpriteProvider SpriteFromColour(Color colour)
		{
			//Rounds `x` to the nearest `n`
			//Aka Round(14,5) => 15
			static byte Round(byte x, byte n)
			{
				//This rounds it to the closest value of `n`, but it might be out of range
				float res = (MathF.Round((float)x / n, MidpointRounding.AwayFromZero) * n);
				//if it's too large, bump it down
				if (res > byte.MaxValue) res -= n;
				return (byte)res;
			}

			/*
			 * 'Round' the colour so that we don't have to check for as many and store as many colours
			 * Essentially reducing the bit depth here
			 * channelUniqueColours is how many unique colours per channel
			 * So if channelUniqueColours == 32, each channel (R,G,B) has 32 unique levels it can be
			 * rounding = 256/32 = 8, so each channel is rounded to the nearest 8
			 * So the total colour count is 32^3=32,768, which is 512 times fewer than full 8-bit per channel
			 * So very good for our memory usage
			 */
			const byte channelUniqueColours = 16;
			const byte rounding = byte.MaxValue / channelUniqueColours;
			colour.R = Round(colour.R, rounding);
			colour.G = Round(colour.G, rounding);
			colour.B = Round(colour.B, rounding);
			colour.A = Round(colour.A, rounding);
			//Try get it from the cache first, as this is the most performant
			if (CachedSpriteFromColour.ContainsKey(colour))
				return CachedSpriteFromColour[colour];

			SpriteFromTexture sprite = new()
			{
					Texture =
							//R8G8B8A8_UNorm_SRgb seems to work without fucking stuff up
							//Because Stride.Core.Mathematics.Color is 1 byte per channel, RGBA
							Texture.New2D(
									GraphicsDevice,
									1,
									1,
									PixelFormat.R8G8B8A8_UNorm_SRgb,
									new[] { colour }
							)
			};
			CachedSpriteFromColour[colour] = sprite;

			return sprite;
		}
	}
}