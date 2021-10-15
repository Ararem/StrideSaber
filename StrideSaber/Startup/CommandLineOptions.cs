using CommandLine;
using CommandLine.Core;

internal class CommandLineOptions
{
	[Option] public bool TestFlag { get; init; } = false;
}