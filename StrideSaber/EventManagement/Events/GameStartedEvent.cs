using Serilog.Events;
using Stride.Engine;
using System;

namespace StrideSaber.EventManagement.Events
{
	/// <summary>
	/// An <see cref="Event"/> for when the game is started (when it actually starts <see cref="Stride.Engine.Game.Run">running</see>)
	/// </summary>
	public sealed class GameStartedEvent : Event
	{
		internal GameStartedEvent(Game game)
		{
			StartTime = DateTime.Now;
			Game = game;
		}

		/// <summary>
		/// The time the game was started at
		/// </summary>
		public readonly DateTime StartTime;

		/// <summary>
		/// The <see cref="Stride.Engine.Game"/> that is running
		/// </summary>
		public readonly Game Game;

		/// <inheritdoc />
		public override string ToString()
		{
			return $"Game started at {StartTime}";
		}

		/// <inheritdoc />
		/// <inheritdoc />
		public override LogEventLevel? FiringLogLevel => LogEventLevel.Information;
	}
}