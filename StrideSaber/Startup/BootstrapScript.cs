using JetBrains.Annotations;
using LibEternal.ObjectPools;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using StrideSaber.SceneManagement;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SLog = Serilog.Log;

#pragma warning disable 8618

namespace StrideSaber.Startup
{
	/// <summary>
	///  An <see cref="AsyncScript"/> that initializes stuff
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class BootstrapScript : AsyncScript
	{
		/// <summary>
		///  Whether the script should automatically start the game
		/// </summary>
		public bool AutoStart = false;

		/// <summary>
		///  The time to wait before starting automatically if <see cref="AutoStart"/> is <see langword="true"/>
		/// </summary>
		public TimeSpan AutoStartWaitDuration;

		/// <summary>
		///  A reference to a <see cref="Scene"/> asset that will be loaded as soon as the script <see cref="Continue">continues</see>
		/// </summary>
		public UrlReference<Scene> MainMenuScene;

		/// <summary>
		///  The <see cref="Scene"/> that should be loaded for the progress UI
		/// </summary>
		public UrlReference<Scene> ProgressUiScene;

		/// <summary>
		///  The <see cref="UIComponent"/> that this object is linked to
		/// </summary>
		public UIComponent Ui;

		/// <summary>
		///  The time in milliseconds between updates of the <see cref="Ui"/>
		/// </summary>
		[DataMemberRange(1, 1000, 1, 50, 0)] public int UpdateInterval = 500;

		/// <inheritdoc/>
		public override async Task Execute()
		{
			SLog.Debug("Bootstrap Script executing");
			//First off, rename our scene
			Entity.Scene.Name = "Bootstrap Scene";

			SLog.Debug("Asynchronously loading Progress UI ({Scene})", ProgressUiScene);
			Scene progressUiScene = await Content.LoadAsync(ProgressUiScene);
			progressUiScene.Name = "Progress Ui";
			SceneSystem.SceneInstance.RootScene.Children.Add(progressUiScene);
			SLog.Debug("Asynchronously loaded Progress Ui  ({Scene})", ProgressUiScene);

			//Init stuff
			UIElement root = Ui.Page.RootElement;
			Button continueButton = root.FindVisualChildOfType<Button>();
			TextBlock countdownText = root.FindVisualChildrenOfType<TextBlock>().First(t => t.Parent is not Button); //The button will have a child TextBlock as well that we need to ignore
			Slider countdownSlider = root.FindVisualChildOfType<Slider>();

			//Log state info
			SLog.Verbose("Continue button:	{@ContinueButton}", continueButton);
			SLog.Verbose("Countdown text:		{@CountdownText}", countdownText);
			SLog.Verbose("Countdown scroll:	{@CountdownScroll}", countdownSlider);
			SLog.Verbose("Auto Start:		{AutoStart}", AutoStart);
			SLog.Verbose("Wait Duration:		{AutoStartWaitDuration}", AutoStartWaitDuration);
			SLog.Verbose("Update Interval:	{UpdateInterval} ms", UpdateInterval);
			SLog.Verbose("Main Menu Scene:	{MainMenuScene}", MainMenuScene);

			//Get our button ready
			continueButton.Click += (_, _) => Task.Run(ContinueButtonOnClick);
			SLog.Verbose("Subscribed continue button click event");

			//If we're not auto-starting (aka waiting for the user)
			//Then there's no point in waiting for anything else
			if (!AutoStart)
			{
				SLog.Verbose("Auto start disabled, waiting for user interaction");
				countdownText.Visibility = countdownSlider.Visibility = Visibility.Hidden;
				return;
			}

			//Ensure our slider scales are correct
			//At the moment, they're in seconds
			countdownSlider.Minimum = (float)TimeSpan.Zero.TotalSeconds;
			countdownSlider.Maximum = (float)AutoStartWaitDuration.TotalSeconds;

			Stopwatch sw = Stopwatch.StartNew();
			TimeSpan remaining;
			StringBuilder sb = StringBuilderPool.GetPooled();
			do
			{
				//Get the remaining time left until we continue automatically
				remaining = AutoStartWaitDuration - sw.Elapsed;
				//Clamp the time so that we don't show negative time
				if (remaining < TimeSpan.Zero)
					remaining = TimeSpan.Zero;
				SLog.Verbose("Time Remaining: {TimeRemaining}", remaining);

				countdownText.Text = sb.Clear().AppendFormat("{0:mm':'ss'.'f}", remaining).ToString();
				countdownSlider.Value = (float)remaining.TotalSeconds;

				await Task.Delay(UpdateInterval);
			} while (remaining > TimeSpan.Zero); //Loop until the time remaining is less than or equal to 0

			//Once we get to here we know the `while()` has finished so we can continue
			StringBuilderPool.ReturnPooled(sb);
			SLog.Verbose("Bootstrap countdown complete, autostarting");
			await Continue();
		}

		private static int hasContinued = 0;
		private async Task Continue()
		{
			//Here, we set the HasContinued field to 1, and read it's value
			//If it's equal to 1 (the 'true' value), we know we've already continued and so there's no point in continuing again, so we can return
			//By using `Interlocked.Exchange` and setting it, we're guaranteed that only one thread/call will get the false value, even if multiple read it at the same time
			//By the way we have to use an int not a bool because it's not supported :(
			if (Interlocked.Exchange(ref hasContinued, 1) == 1)
			{
				SLog.Warning("Continue() recalled, ignoring");
				return;
			}
			SLog.Information("Continue() called, continuing");

			//We can't swap scenes as this is the root scene, but we can get rid of the components in this one
			//Remove the entities in the scene that don't have any camera components
			//Because the camera needs to be reused for the next scene
			SLog.Verbose("Marking scene entities for clearing");
			var allEntities = Entity.Scene.Entities;
			List<Entity> toRemove = new(allEntities.Count);
			for(int i=0; i< allEntities.Count; i++)
			{
				Entity entity = allEntities[i];
				//If it has a camera, skip it and don't clear
				if (entity.Components.Any(static c => c is CameraComponent))
				{
					SLog.Verbose("Skipping camera entity {$Entity}", entity);
				}
				//Not camera, remove it the faster way
				else
				{
					SLog.Verbose("Marking entity {$Entity}", entity);
					toRemove.Add(entity);
				}
			}

			SLog.Verbose("Clearing {Count} entities: {List}", toRemove.Count, toRemove.Select(e => e.ToString()));
			foreach (Entity entity in toRemove) allEntities.Remove(entity);
			await SceneUtils.LoadSceneAsync(Content, SceneSystem, MainMenuScene);
		}

		private async Task ContinueButtonOnClick()
		{
			SLog.Verbose("User pressed continue button, continuing");
			await Continue();
		}
	}
}