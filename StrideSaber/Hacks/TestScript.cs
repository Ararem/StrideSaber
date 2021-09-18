using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using System;
using System.Threading.Tasks;

namespace StrideSaber.Hacks
{
	public class TestScript : AsyncScript
	{
		private static GameWindow Window;

		[EventMethod(typeof(GameStartedEvent))]
		private static void SetWindow(Event e)
		{
			Window = (e as GameStartedEvent)!.Game.Window;
		}

		/// <inheritdoc />
		public override async Task Execute()
		{
			int x = 960, y = 540;
			int flag = 0;
			int speed = 4;
			while (true)
			{
				await Task.Delay(10);
				if (Window is null)
				{
					Serilog.Log.Information("No window yet :(");
					continue;
				}
				switch (flag)
				{
					case 1:
						x += speed;
						break;
					case 2:
						y += speed;
						break;
					case 3:
						x -= speed;
						break;
					case 4:
						y -= speed;
						break;
					case 5:
						break;
					case 6:
						Window.SetSize(Window.PreferredWindowedSize);
						break;
				}

				if (x > 1280)
					x = 960;
				if (y > 720)
					y = 540;

				//If a console key was pressed, toggle our flag
				if (Console.KeyAvailable)
				{
					Console.ReadKey();
					flag++;
					if (flag > 6) flag = 1;
				}

				if ((flag != 6) && (flag != 5))
					Window.SetSize(new Int2(x, y));
				Serilog.Log.Information("Window: Bounds={WindowResolution}\tTarget={WindowTarget}\tFlag={Flag}", Window.ClientBounds.Size, Window.PreferredWindowedSize, flag);
			}
		}
	}
}