using JetBrains.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Rendering;
using StrideSaber.EventManagement;
using StrideSaber.EventManagement.Events;
using System;
using System.Collections.Generic;

namespace StrideSaber.Hacks
{
	/// <summary>
	///  A script component that automatically adjusts the aspect ratio of the attached <see cref="UIComponent"/> according the the
	///  <see cref="Rectangle.Size"/>
	/// </summary>
	/// <see cref="UIComponent"/>
	[UsedImplicitly]
	[RequireComponent(typeof(UIComponent))]
	public class AutoAspectRatioComponent : StartupScript
	{
		/// <summary>
		/// A hashset that weakly references all the current <see cref="AutoAspectRatioComponent"/> instances
		/// </summary>
		private static readonly HashSet<WeakReference<AutoAspectRatioComponent>> Instances = new();

		/// <summary>
		/// The <see cref="UIComponent"/> that this object should modify
		/// </summary>
		private UIComponent ui = null!;

		/// <inheritdoc/>
		public override void Start()
		{
			ui = EnsureEntity.Get<UIComponent>();
			Instances.Add(new WeakReference<AutoAspectRatioComponent>(this));
		}

		/// <inheritdoc/>
		public override void Cancel()
		{
			//Remove any weak references where the weak reference points to this object (cause it's being destroyed)
			Instances.RemoveWhere(match => match.TryGetTarget(out var component) && (component == this));
		}

		[EventMethod(typeof(GameStartedEvent))]
		private static void AllowAutoAspect(GameStartedEvent e)
		{
			Game g = e.Game;
			//Whenever our game window has it's size changed
			g.Window.ClientSizeChanged += (sender, _) =>
			{
				//Remove any invalid instances
				//Do this by checking if the weak reference no longer points to an object
				Instances.RemoveWhere(w => w.TryGetTarget(out var _) == false);
				foreach (WeakReference<AutoAspectRatioComponent> weakRef in Instances)
					if (weakRef.TryGetTarget(out var component))
						SetCorrectAspect((GameWindow) sender!, component.ui);
			};
		}

		private static void SetCorrectAspect(GameWindow window, UIComponent ui)
		{
			//These aspects are how many units wide for each unit high
			Vector2 winSize = new(window.ClientBounds.Size.Width, window.ClientBounds.Size.Height);
			Vector2 uiSize = new(ui.Resolution.X, ui.Resolution.Y);
			float windowAspect = winSize.X / winSize.Y;
			float uiAspect = uiSize.X / uiSize.Y;
			//If the screen is too wide we want to fix the height so that it doesn't go out of bounds
			if (windowAspect > uiAspect)
				ui.ResolutionStretch = ResolutionStretch.FixedHeightAdaptableWidth;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			else if (windowAspect == uiAspect)
				ui.ResolutionStretch = ResolutionStretch.FixedWidthFixedHeight;
			else if(windowAspect < uiAspect)
				ui.ResolutionStretch = ResolutionStretch.FixedWidthAdaptableHeight;
			Serilog.Log.Verbose("[Window {WindowName}] {WindowResolution}\t{WindowAspect} w/h\t[Ui {UiName}] {UiResolution}\t{UiAspect} w/h\tMode = {StretchMode}", window.Name, winSize, windowAspect, ui.Entity.Name, uiSize, uiAspect, ui.ResolutionStretch);
		}
	}
}