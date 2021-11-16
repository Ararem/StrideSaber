using JetBrains.Annotations;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Events;
using System.Threading;
using System.Threading.Tasks;
using SLog = Serilog.Log;

namespace StrideSaber.SceneSpecific.Main_Menu
{
	/// <summary>
	/// A script that controls the main menu for the game
	/// </summary>
	public sealed class MainMenuUiController : StartupScript
	{
		/// <summary>
		/// The <see cref="UIComponent"/> that this component will be controlling
		/// </summary>
		[UsedImplicitly]
		public UIComponent Ui = null!;

		private Button devModeButton = null!;
		private Button testButton = null!;

		/// <inheritdoc />
		public override void Start()
		{
			devModeButton = Ui.Page.RootElement.FindVisualChildOfType<Button>("Dev Mode Button");
			testButton = Ui.Page.RootElement.FindVisualChildOfType<Button>("Test Button");
			SLog.Verbose("Dev Mode Button is {@DevModeButton}", devModeButton);
			SLog.Verbose("Test Button is {@TestButton}", testButton);

			testButton.Click    += TestButton_OnClick;
			devModeButton.Click += (sender, args) => SLog.Information("DevModeButton::Click(): {@Sender} {@Args}", sender, args);
		}

		private async void TestButton_OnClick(object sender, RoutedEventArgs args)
		{
			SLog.Information("TestButton::Click(): {@Sender} {@Args}", sender, args);
			testButton.Visibility = Visibility.Collapsed;
			await Task.Delay(2000);
			testButton.Visibility = Visibility.Visible;
		}
	}
}