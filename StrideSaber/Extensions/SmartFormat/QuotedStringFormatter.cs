using LibEternal.ObjectPools;
using SmartFormat.Core.Extensions;

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
				//We have to write in one 'pass' due to how the alignment works internally
				formattingInfo.Write(StringBuilderPool.BorrowInline(static (sb, s) =>
				{
					//We might be working with big strings, so ensure it's big enough
					sb.EnsureCapacity(s.Length + 2);
					sb.Append('"');
					sb.Append(s);
					sb.Append('"');
				}, s));
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