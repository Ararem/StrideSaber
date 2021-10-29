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
			Task.Run(AsyncTaskCreator);
			//(Game as Game)!.WindowMinimumUpdateRate.SetMaxFrequency(10);
			SLog.Information("Task creator started");
		}

		private static Random r = new();

		[SuppressMessage("ReSharper", "All")]
		private static async Task AsyncTaskCreator()
		{
			_ = new TrackedTask("Fps", FpsTask) { Id = Guid.Empty };
			int i = 0;
			while (true)
			{
				int delay = r.Next(1000, 2000);
				await Task.Delay(delay);
				if (TrackedTask.UnsafeInstances.Count < 10)
					_ = new TrackedTask($"Test task {++i}", AsyncTaskTest);
			}
		}

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
			DateTime end = start + TimeSpan.FromMilliseconds(r.Next(1000, 15000));
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

		/// <inheritdoc />
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
				sb.Append($"{task.Name}:\t{task.Progress,3:p0}");
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
				slider.TrackBackgroundImage = SpriteFromColour(backgroundColour, true);
				slider.TickImage = SpriteFromColour(tickColour, true);
				slider.TrackForegroundImage = SpriteFromColour(uniqueTaskColour, false);
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

		private ISpriteProvider SpriteFromColour(Color colour, bool storeInCache)
		{
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

			if (storeInCache)
				CachedSpriteFromColour[colour] = sprite;

			return sprite;
		}
	}
}