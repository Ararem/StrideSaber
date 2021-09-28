using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using SLog = Serilog.Log;

namespace StrideSaber.SceneSpecific.Main_Menu
{
	public sealed class MainMenuUiController : StartupScript
	{
		public UIComponent Ui;

		private Button devModeButton;
		private Button testButton;

		/// <inheritdoc />
		public override void Start()
		{
			devModeButton = Ui.Page.RootElement.FindVisualChildOfType<Button>("Dev Mode Button");
			testButton = Ui.Page.RootElement.FindVisualChildOfType<Button>("Test Button");
			SLog.Verbose("Dev Mode Button is {@DevModeButton}", devModeButton);
			SLog.Verbose("Test Button is {@TestButton}", testButton);

			testButton.Click += (sender, args) => SLog.Information("TestButton::Click(): {Sender} {@Args}", sender, args);
			devModeButton.Click += (sender, args) => SLog.Information("DevModeButton::Click(): {Sender} {@Args}", sender, args);
		}
	}
}