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
		/// The reference to the UI library used to create task display components
		/// </summary>
		public UrlReference<UILibrary> LibraryReference = null!;

		private UILibrary  library         = null!;
		private StackPanel indicatorsPanel = null!;

	#region Testing

		/// <inheritdoc />
		public override void Start()
		{
			indicatorsPanel = Ui.Page.RootElement.FindVisualChildOfType<StackPanel>("Indicators");
			//Have to clear the indicators panel because i filled it in the editor for testing
			indicatorsPanel.Children.Clear();
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
			_ = new TrackedTask("Fps",        FpsTask) { Id       = Guid.Empty };
			_ = new TrackedTask("Task Count", TaskCountTask) { Id = Guid.Empty };
			int i = 0;
			while (true)
			{
				//Wait a while between each task instantiation
				int delay = r.Next(1, 10);
				await Task.Delay(delay);
				//Ensure not too many tasks
				if (TrackedTask.UnsafeInstances.Count < 500)
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
		private static async Task TaskCountTask(Action<float> updateProgress)
		{
			while (true)
			{
				updateProgress(TrackedTask.UnsafeInstances.Count / 100f);
				await Task.Delay(100);
			}
		}

		[SuppressMessage("ReSharper", "All")]
		private static async Task AsyncTaskTest(Action<float> updateProgress)
		{
			DateTime start = DateTime.Now;
			DateTime end   = start + TimeSpan.FromMilliseconds(r.Next(1000, 20000));
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
		/// The task displays that are currently being shown right now
		/// </summary>
		private readonly Queue<TaskDisplay> shownTaskDisplays = new();

		/// <summary>
		/// Cached display of task displays.
		/// </summary>
		/// <remarks>
		///	This stores instances so that they don't need to be re-allocated/created each time we want to add a new task (each instance is relatively large).
		/// </remarks>
		private readonly Queue<TaskDisplay> cachedTaskDisplays = new();

		/// <summary>
		/// Updates the UI from the current tasks and their statuses
		/// </summary>
		public override void Update()
		{
			//First off, move all the current displays into the cache
			while (shownTaskDisplays.TryDequeue(out var display)) cachedTaskDisplays.Enqueue(display);
			//Optimisation: Instead of looping over all the displays and removing them individually, just clear the list
			//Could cause issues if other child UI elements are present, but there shouldn't be any so assume it's fine
			indicatorsPanel.Children.Clear();

			foreach (TrackedTask task in TrackedTask.ToArrayPoolArray())
			{
				//Try pull out an existing display, else create a new one
				#pragma warning disable 8600
				if (!cachedTaskDisplays.TryDequeue(out TaskDisplay display))
					display = new TaskDisplay(this);
				#pragma warning restore 8600

				//Update the display from the current task
				display.Update(task, Game.GraphicsContext);

				//We have an updated task display element, add it to the panel so that it gets drawn
				indicatorsPanel.Children.Add(display.IndicatorRootElement);
				//Also add it to the Queue so we can track it
				shownTaskDisplays.Enqueue(display);
			}
		}

		/// <summary>
		/// A object that stores references for a task display so that objects can be reused.
		/// </summary>
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
			{
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
				IndicatorRootElement.BorderColor = uniqueTaskColour;
				//Thankfully we have the textures cached so we don't need to re-fetch them
				//And now since they're 1x1 textures we can simply overwrite their colours
				SetTexDataUnsafe(background, graphicsContext.CommandList, backgroundColour);
				SetTexDataUnsafe(foreground, graphicsContext.CommandList, uniqueTaskColour);
				SetTexDataUnsafe(tick,       graphicsContext.CommandList, tickColour);
			}

			/*
			 * The reason I'm doing this unsafe and using pointers is because when I was testing with large amounts of tasks,
			 * this was allocating large amounts of memory (>50mb) from just the colour array alone in under a minute
			 */
			private static unsafe void SetTexDataUnsafe(Texture tex, CommandList commandList, Color colour)
			{
				//Stack allocate so we can use pointers and avoid memory allocations
				Span<Color> colourSpan = stackalloc Color[1] { colour };
				fixed (Color* ptr = colourSpan)
				{
					tex.SetData(commandList, new DataPointer(ptr, sizeof(Color)));
				}
			}

			~TaskDisplay()
			{
				SLog.Verbose("TaskDisplay::Finalizer()");
				//Dispose of the textures when this object is destroyed
				background.Dispose();
				tick.Dispose();
				foreground.Dispose();
				slider.TrackBackgroundImage = slider.TrackForegroundImage = slider.TickImage = null;
			}
		}
	}
}