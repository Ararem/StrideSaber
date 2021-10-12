using OpenTK;
using Stride.Engine;
using StrideSaber.Diagnostics;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using SLog = Serilog.Log;

namespace StrideSaber.SceneSpecific.Progress_Ui
{
	public class ProgressUiController : SyncScript
	{
		public UIComponent Ui;
		public UILibrary Library;

		/// <inheritdoc />
		public override void Start()
		{
			Task.Run(AsyncTaskCreator);
			(Game as Game).WindowMinimumUpdateRate.SetMaxFrequency(1);
			SLog.Information("Task creator started");
		}

		static Random r = new();

		private static async Task AsyncTaskCreator()
		{
			int i = 0;
			//while (true)
			{
				int delay = r.Next(1000, 5000);
				await Task.Delay(delay);
				if (BackgroundTask.UnsafeInstances.Count < 5)
					_ = new BackgroundTask($"Test task {++i}", AsyncTaskTest);
			}
		}

		private static async Task AsyncTaskTest(Action<float> updateProgress)
		{
			DateTime start = DateTime.Now;
			DateTime end = start + TimeSpan.FromMilliseconds(r.Next(250, 5000));
			while (DateTime.Now < end)
			{
				await Task.Delay(1);
				updateProgress((float)((DateTime.Now - start) / (end - start)));
			}
			updateProgress(1);
		}

		/// <inheritdoc />
		public override void Update()
		{
			//Get all the current tasks
			var tasks = BackgroundTask.UnsafeInstances;
			//Console.Clear();
			foreach (var task in tasks)
				SLog.Debug("{Task}",task.ToStringBar());
		}
	}
}