using JetBrains.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using System;
using System.Collections.Generic;

namespace StrideSaber.Hacks
{
	/// <summary>
	///	A script component that automatically adjusts the aspect ratio of the attached <see cref="UIComponent"/> according the the <see cref="Rectangle.Size"/>
	/// </summary>
	[UsedImplicitly]
	[RequireComponent(typeof(UIComponent))]
	public class AutoAspectRatioComponent : StartupScript
	{
		private static readonly HashSet<WeakReference<AutoAspectRatioComponent>> Instances = new();
		private UIComponent ui = null!;

		/// <inheritdoc />
		public override void Start()
		{
			ui = EnsureEntity.Get<UIComponent>();
			Instances.Add(new WeakReference<AutoAspectRatioComponent>(this));
		}

		/// <inheritdoc />
		public override void Cancel()
		{
			//Remove any weak references where the weak reference points to this object (cause it's being destroyed)
			Instances.RemoveWhere(match => match.TryGetTarget(out var component) && (component == this));
		}

		[EventMethod(typeof(GameStartedEvent))]
		private static void AllowAutoAspect(Event e)
		{
			Game g = ((GameStartedEvent) e).Game;
			//Whenever our game window has it's size changed
			g.Window.ClientSizeChanged += (sender, _) =>
			{
				//Remove any invalid instances
				//Do this by checking if the weak reference no longer points to an object
				Instances.RemoveWhere(w => w.TryGetTarget(out var _) == false);
				foreach (WeakReference<AutoAspectRatioComponent> weakRef in Instances)
					if(weakRef.TryGetTarget(out var component))
						SetCorrectAspect((GameWindow) sender!, component.ui);
			};
		}

		private static void SetCorrectAspect(GameWindow window, UIComponent ui)
		{
			//These aspects are how many units wide for each unit high
			float windowAspect = (float) window.ClientBounds.Width / window.ClientBounds.Height;
			float uiAspect = ui.Resolution.X / ui.Resolution.Y;
			Serilog.Log.Information("Aspect: Window={WindowAspect:n1}\t\tUi={UiAspect:n1}", windowAspect, uiAspect);
			Serilog.Log.Information("Window={WindowResolution}\tUi={UiResolution}", window.ClientBounds, ui.Resolution);
		}
	}
}