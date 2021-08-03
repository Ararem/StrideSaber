﻿using JetBrains.Annotations;
using System.Diagnostics.CodeAnalysis;

namespace StrideSaber.Core.Settings
{
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	[UsedImplicitly]
	[PublicAPI]
	public class TestSettings : ISettingsClass
	{
		public string String_Prop => nameof(String_Prop);
	}
}