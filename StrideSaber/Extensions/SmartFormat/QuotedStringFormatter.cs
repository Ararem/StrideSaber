using SmartFormat.Core.Extensions;
using System;
using System.Buffers;

namespace StrideSaber.Extensions.SmartFormat
{
	/// <summary>
	/// A formatter that wraps strings in quotes: "String"
	/// </summary>
	public sealed class QuotedStringFormatter : IFormatter
	{
		/// <inheritdoc />
		public bool TryEvaluateFormat(IFormattingInfo formattingInfo)
		{
			if (formattingInfo.CurrentValue is string s)
			{
				formattingInfo.Write("\"");
				formattingInfo.Write(s);
				formattingInfo.Write("\"");
				return true;
			}

			return false;
		}

		/// <inheritdoc />
		public string Name { get; set; } = "quoted";

		/// <inheritdoc />
		public bool CanAutoDetect { get; set; } = false;
	}
}