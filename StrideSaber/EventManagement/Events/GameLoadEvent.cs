using Serilog.Events;
using Stride.Engine;
using System;

namespace StrideSaber.EventManagement.Events
{
	/// <summary>
	/// An <see cref="Event"/> that marks when the game is first loaded. This would normally be called just after the <see cref="Stride.Engine.Game"/> has been constructed, but before it is actually run.
	/// </summary>
	public sealed class GameLoadEvent : Event
	{
		internal GameLoadEvent(Game game)
		{
			LoadTime = DateTime.Now;
			Game = game;
		}

		/// <summary>
		/// The time the game was loaded at
		/// </summary>
		public readonly DateTime LoadTime;

		/// <summary>
		/// The <see cref="Stride.Engine.Game"/> that is running
		/// </summary>
		public readonly Game Game;

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Game loaded at {LoadTime}";
		}

		/// <inheritdoc />
		/// <inheritdoc />
		public override LogEventLevel? FiringLogLevel => LogEventLevel.Information;
	}
}