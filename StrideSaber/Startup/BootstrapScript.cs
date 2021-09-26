using JetBrains.Annotations;
using LibEternal.ObjectPools;
using Serilog.Context;
using Stride.Core.Annotations;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SLog = Serilog.Log;
#pragma warning disable 8618

namespace StrideSaber.Startup
{
	/// <summary>
	/// An <see cref="AsyncScript"/> that initializes stuff
	/// </summary>
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class BootstrapScript : AsyncScript
	{
		/// <summary>
		/// Whether the script should automatically start the game
		/// </summary>
		public bool AutoStart = false;

		/// <summary>
		/// The time to wait before starting automatically if <see cref="AutoStart"/> is <see langword="true"/>
		/// </summary>
		public TimeSpan AutoStartWaitDuration;

		/// <summary>
		/// The <see cref="UIComponent"/> that this object is linked to
		/// </summary>
		public UIComponent Ui;

		/// <summary>
		/// A reference to a <see cref="Scene"/> asset that will be loaded as soon as the script <see cref="Continue">continues</see>
		/// </summary>
		public UrlReference<Scene> MainMenuScene;

		/// <summary>
		/// The time in milliseconds between updates of the <see cref="Ui"/>
		/// </summary>
		[DataMemberRange(1, 1000, 1, 50, 0)] public int UpdateInterval = 100;

		/// <inheritdoc/>
		public override async Task Execute()
		{
			SLog.Debug("Bootstrap Script executing");
			//Init stuff
			UIElement root = Ui.Page.RootElement;
			Button continueButton = root.FindVisualChildOfType<Button>();
			TextBlock countdownText = root.FindVisualChildrenOfType<TextBlock>().First(t => t.Parent is not Button); //The button will have a child TextBlock as well that we need to ignore
			Slider countdownSlider = root.FindVisualChildOfType<Slider>();

			//Log state info
			SLog.Verbose("Continue button:		{@ContinueButton}", continueButton);
			SLog.Verbose("Countdown text:		{@CountdownText}", countdownText);
			SLog.Verbose("Countdown scroll:	{@CountdownScroll}", countdownSlider);
			SLog.Verbose("Auto Start:			{AutoStart}", AutoStart);
			SLog.Verbose("Wait Duration:		{AutoStartWaitDuration}", AutoStartWaitDuration);
			SLog.Verbose("Update Interval:		{UpdateInterval}", UpdateInterval);
			SLog.Verbose("Main Menu Scene :	{MainMenuScene}", MainMenuScene);

			//Get our button ready
			continueButton.Click += ContinueButtonOnClick;

			//If we're not auto-starting (aka waiting for the user)
			//Then there's no point in waiting for anything else
			if (!AutoStart)
			{
				SLog.Verbose("Auto start disabled, waiting for user interaction");
				countdownText.IsEnabled = false;
				countdownSlider.IsEnabled = false;
				return;
			}

			//Ensure our slider scales are correct
			//At the moment, they're in seconds
			countdownSlider.Minimum = (float) TimeSpan.Zero.TotalSeconds;
			countdownSlider.Maximum = (float) AutoStartWaitDuration.TotalSeconds;
			//TODO: Make this scale properly with the wait duration
			countdownSlider.Step = 1F;
			countdownSlider.TickFrequency = 10F;
			SLog.Verbose("Scroll tick frequency is {Frequency}", countdownSlider.TickFrequency);

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
				countdownSlider.Value = (float) remaining.TotalSeconds;

				await Task.Delay(UpdateInterval);

				#if true
				#warning LOOP: TESTING ONLY
				if (remaining <= TimeSpan.Zero)
					sw.Restart();
			} while (true);
			#else
			} while (remaining > TimeSpan.Zero); //Loop until the time remaining is less than or equal to 0
			#endif

			//Once we get to here we know the `while()` has finished so we can continue
			StringBuilderPool.ReturnPooled(sb);
			SLog.Verbose("Bootstrap countdown complete, autostarting");
			Continue();
		}

		private void Continue()
		{
			SLog.Information("Continue() called");
			//Unload old scene
			SLog.Verbose("Unloading old scene");
			Content.Unload(Entity.Scene);
			SLog.Verbose("Old scene unloaded");
			//Load the new scene
			SLog.Verbose("Loading new scene");
			Content.LoadAsync(MainMenuScene);
			SLog.Verbose("Loaded new scene");
		}

		private void ContinueButtonOnClick(object? sender, RoutedEventArgs e)
		{
			e.Handled = true;
			SLog.Verbose("User pressed continue button, continuing");
			Continue();
		}
	}
}