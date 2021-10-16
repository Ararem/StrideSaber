using CommandLine;
using CommandLine.Core;

internal class CmdOptions
{
	[Option] public bool TestFlag { get; init; } = false;
}