using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Text;

namespace StrideSaber.Core.SourceGenerators
{
	[Generator]
	public sealed class StaticInstanceMembersGenerator : ISourceGenerator
	{
		private static readonly DiagnosticDescriptor warn;

		/// <inheritdoc />
		public void Initialize(GeneratorInitializationContext ctx)
		{
			// Register a factory that can create our custom syntax receiver
			ctx.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		/// <inheritdoc />
		public void Execute(GeneratorExecutionContext ctx)
		{
			//Write the log entries
			ctx.AddSource("Logs", SourceText.From($@"/*{ Environment.NewLine + string.Join(Environment.NewLine, Log) + Environment.NewLine}*/", Encoding.UTF8));
		}

		/// <summary>
		/// Stores messages we need to log later
		/// </summary>
		private static readonly List<string> Log = new();

		/// <inheritdoc />
		private sealed class SyntaxReceiver : ISyntaxReceiver
		{
			/// <inheritdoc />
			public void OnVisitSyntaxNode(SyntaxNode node)
			{
				try
				{
					if (node is ClassDeclarationSyntax cds) Log.Add($"Found a class named {cds.Identifier.ValueText}");
				}
				catch (Exception ex)
				{
					Log.Add("Error parsing syntax: " + ex);
				}
			}
		}
	}
}