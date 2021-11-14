using JetBrains.Annotations;
using LibEternal.ObjectPools;
using Stride.Core.Extensions;
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

		private UILibrary  library         = null!;
		private StackPanel indicatorsPanel = null!;

	#region Testing

		/// <inheritdoc />
		public override void Start()
		{
			indicatorsPanel = Ui.Page.RootElement.FindVisualChildOfType<StackPanel>("Indicators");
			library         = Content.Load(LibraryReference);
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
			DateTime end   = start + TimeSpan.FromMilliseconds(r.Next(0, 5000));
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
				tickColour       = new(211, 211, 211, 255);

		/// <summary>
		/// The tasks that are currently being shown right now
		/// </summary>
		private readonly Queue<TaskDisplay> shownTaskDisplays = new();

		private readonly Queue<TaskDisplay> cachedTaskDisplays = new();

		/// <summary>
		/// Updates the UI from the current tasks and their statuses
		/// </summary>
		public override void Update()
		{
			//Get all the current tasks, ordered by ID
			var tasks = TrackedTask.CloneAllInstances()
									.OrderBy(static t => t.Id)
									.ToArray();

			//First off, move all the current displays into the cache
			while (shownTaskDisplays.TryDequeue(out var display))
			{
				//Remove it from the visual tree so we don't see it
				indicatorsPanel.Children.SwapRemove(display.IndicatorRootElement);
				cachedTaskDisplays.Enqueue(display);
			}

			for (int i = 0; i < tasks.Length; i++)
			{
				TrackedTask task = tasks[i];
				//Try pull out an existing display, else create a new one
				#pragma warning disable 8600
				if (!cachedTaskDisplays.TryDequeue(out TaskDisplay display))
					display = new TaskDisplay(this);
				#pragma warning restore 8600


				//Update the display from the current task
				display.Update(task, Game.GraphicsContext);

				//We have an updated task display element, add it to the panel so that it gets drawn
				indicatorsPanel.Children.Add(display.IndicatorRootElement);
			}
		}

		private class TaskDisplay
		{
			/// <summary>
			/// Cached reference to the slider child element
			/// </summary>
			private readonly Slider slider;

			/// <summary>
			/// The <see cref="Texture">Texture</see> for the <see cref="slider"/> section
			/// </summary>
			private readonly Texture background, foreground, tick;

			/// <summary>
			/// Cached reference to the text block child.
			/// </summary>
			private readonly TextBlock textBlock;

			/// <summary>
			/// The root <see cref="UIElement"/> for this task display. Should be a child of the indicators panel when being displayed (not cached)
			/// </summary>
			internal readonly Border IndicatorRootElement;

			internal TaskDisplay(ProgressUiController controller)
			{z
				IndicatorRootElement = controller.library.InstantiateElement<Border>("Progress Indicator");
				//I don't like the use of LINQ on a possible hot-path, but because of the way things are nested I have no alternative
				slider    = IndicatorRootElement.FindVisualChildOfType<Slider>();
				textBlock = IndicatorRootElement.FindVisualChildOfType<TextBlock>();

				//Create and assign our textures for the slider
				foreground = NewTex(controller.GraphicsDevice);
				tick       = NewTex(controller.GraphicsDevice);
				background = NewTex(controller.GraphicsDevice);

				slider.TrackForegroundImage = new SpriteFromTexture { Texture = foreground };
				slider.TrackBackgroundImage = new SpriteFromTexture { Texture = background };
				slider.TickImage            = new SpriteFromTexture { Texture = tick };

				static Texture NewTex(GraphicsDevice graphicsDevice)
				{
					//R8G8B8A8_UNorm_SRgb seems to work without fucking stuff up
					//Because Stride.Core.Mathematics.Color is 1 byte per channel, RGBA
					return Texture.New2D(
							graphicsDevice,
							1,
							1,
							PixelFormat.R8G8B8A8_UNorm_SRgb,
							new[] { Color.Purple }, //Purple is the default so tha twe can easily tell if we forgot to update it
							usage: GraphicsResourceUsage.Dynamic
					);
				}
			}

			// PERF: Hot path, needs lots of performance improvements, namely:
			// 1: Task string formatting handling
			// 2: Child finding for components - Uses LINQ
			// 3: Almost nothing is cached, this would be a massive improvement
			// 4: Colours for sliders - perhaps internally create a list of ~1000 combinations and pull out the closest approximation?
			// Texture reuse - `tex.SetData(Game.GraphicsContext.CommandList, new[]{colour});` Needs texture to have `GraphicsResourceUsage.Dynamic`
			internal void Update(TrackedTask task, GraphicsContext graphicsContext)
			{
				//Set the text and slider values for the task
				//Using a StringBuilder is ~50% slower than interpolation, but ~40% less memory
				textBlock.Text = StringBuilderPool.BorrowInline(static (sb, task) =>
								sb.Append(task.Name)
								.Append(":\t")
								.Append(task.Progress.ToString("p0").PadLeft(3))
						, task);
				slider.Value = task.Progress;

				//Try and give the task a nice little colour too
				//This one is based on the hash of the task ID
				Color uniqueTaskColour = Color.FromRgba(task.Id.GetHashCode());
				uniqueTaskColour.A = 255; //Ensure we can see the task so set the alpha to full
				//TODO: This is decent for now, but maybe fix the tick lines (contrasting to fg and bg)
				//FIXME: Do we meed to dispose of the old value here? Was a memory leak but i did change this a lot
				IndicatorRootElement.BorderColor = uniqueTaskColour;
				//Thankfully we have the textures cached so we don't need to re-fetch them
				//And now since they're 1x1 textures we can simply overwrite their colours
				background.SetData(graphicsContext.CommandList, new[] { backgroundColour });
				foreground.SetData(graphicsContext.CommandList, new[] { uniqueTaskColour });
				tick.SetData(graphicsContext.CommandList, new[] { tickColour });
			}
		}
	}
}