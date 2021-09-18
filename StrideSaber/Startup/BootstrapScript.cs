using LibEternal.ObjectPools;
using Stride.Core.Annotations;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace StrideSaber.Startup
{
	public class BootstrapScript : AsyncScript
	{
		public bool AutoStart = false;
		public TimeSpan AutoStartWaitDuration;
		public UIComponent Ui;
		[DataMemberRange(1, 1000, 1, 50, 0)] public int UpdateInterval = 100;

		/// <inheritdoc />
		public override async Task Execute()
		{
			Serilog.Log.Debug("Bootstrap Script executing");
			//Init stuff
			UIElement root = Ui.Page.RootElement;
			Button continueButton = root.FindVisualChildOfType<Button>();
			TextBlock countdownText = root.FindVisualChildOfType<TextBlock>();
			Slider countdownSlider = root.FindVisualChildOfType<Slider>();
			Serilog.Log.Verbose("Continue button  is {@ContinueButton}", continueButton);
			Serilog.Log.Verbose("Countdown text   is {@CountdownText}", countdownText);
			Serilog.Log.Verbose("Countdown scroll is {@CountdownScroll}", countdownSlider);

			//Log state info
			Serilog.Log.Verbose("Auto Start: {AutoStart}", AutoStart);
			Serilog.Log.Verbose("Wait Duration: {AutoStartWaitDuration}", AutoStartWaitDuration);
			Serilog.Log.Verbose("Update Interval: {UpdateInterval}", UpdateInterval);

			//Get our button ready
			continueButton.Click += ContinueButtonOnClick;

			//If we're not auto-starting (aka waiting for the user)
			//Then there's no point in waiting for anything else
			if (!AutoStart)
			{
				Serilog.Log.Verbose("Auto start disabled, waiting for user interaction");
				countdownText.IsEnabled = false;
				countdownSlider.IsEnabled = false;
				return;
			}

			Stopwatch sw = Stopwatch.StartNew();
			TimeSpan remaining;
			StringBuilder sb = StringBuilderPool.GetPooled();
			do
			{
				//Get the remaining time left until we continue automatically
				remaining = AutoStartWaitDuration - sw.Elapsed;
				countdownText.Text = sb.Clear().AppendFormat("{0}", remaining).ToString();
				Serilog.Log.Verbose("Time Remaining: {TimeRemaining}", remaining);
				await Task.Delay(UpdateInterval);
			} while (remaining > TimeSpan.Zero);

			//Once we get to here we know the `while()` has finished so we can continue
			StringBuilderPool.ReturnPooled(sb);
			Continue();
		}

		private void Continue()
		{
		}

		private void ContinueButtonOnClick(object? sender, RoutedEventArgs e)
		{
			e.Handled = true;
			Continue();
		}
	}
}