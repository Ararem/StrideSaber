using JetBrains.Annotations;
using LibEternal.ObjectPools;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using StrideSaber.Diagnostics;
using StrideSaber.Startup;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLog = Serilog.Log;

namespace StrideSaber.SceneSpecific.Progress_Ui
{
	/// <summary>
	/// A script that controls UI elements to display the progress of all the currently running <see cref="BackgroundTask"/> instances
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

		[SuppressMessage("ReSharper","All")]
		private static async Task AsyncTaskCreator()
		{
			_ = new BackgroundTask("Fps", FpsTask);
			int i = 0;
			while (true)
			{
				int delay = r.Next(1000, 7000);
				await Task.Delay(delay);
				if (BackgroundTask.UnsafeInstances.Count < 10)
					_ = new BackgroundTask($"Test task {++i}", AsyncTaskTest);
			}
		}

		[SuppressMessage("ReSharper","All")]
		private static async Task FpsTask(Action<float> updateProgress)
		{
			while (true)
			{
				updateProgress(StrideSaberApp.CurrentGame.UpdateTime.FramePerSecond/100);
				await Task.Delay(1);
			}
		}

		[SuppressMessage("ReSharper","All")]
		private static async Task AsyncTaskTest(Action<float> updateProgress)
		{
			DateTime start = DateTime.Now;
			DateTime end = start + TimeSpan.FromMilliseconds(r.Next(0, 15000));
			while (DateTime.Now < end)
			{
				await Task.Delay(1);
				updateProgress((float)((DateTime.Now - start) / (end - start)));
			}
			updateProgress(1);
		}

		#endregion

		/// <inheritdoc />
		public override void Update()
		{
			//Get all the current tasks
			var tasks = BackgroundTask.UnsafeInstances
			                          .OrderBy(t=>t.Name)
			                          .ToArray();
			//Now update the UI
			StringBuilder sb = StringBuilderPool.GetPooled();
			for (int i = 0; i < tasks.Length; i++)
			{
				UIElement indicator;
				BackgroundTask task = tasks[i];
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
				sb.Clear();
				sb.AppendFormat("{0}:\t{1,3:p0}", task.Name, task.Progress);
				indicator.FindVisualChildOfType<TextBlock>().Text = sb.ToString();
				indicator.FindVisualChildOfType<Slider>().Value = task.Progress;
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
	}
}