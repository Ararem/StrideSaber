using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StrideSaber.SourceGenerators
{
	/// <inheritdoc />
	[Generator]
	public sealed class StaticInstanceMembersGenerator : ISourceGenerator
	{
		/// <inheritdoc />
		public void Initialize(GeneratorInitializationContext ctx)
		{
			// Register a factory that can create our custom syntax receiver
			ctx.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
		}

		/// <inheritdoc />
		public void Execute(GeneratorExecutionContext ctx)
		{
			//From my understanding, the syntax reciever is the "scan" phase that finds stuff to work on,
			//and the "execute" is where we actually do the work

			Log("Path is:");
			Log(Path.GetFullPath("./"));

			//Here we write to our log file
			ctx.AddSource("Logs", SourceText.From($@"/*
{string.Join("\n", _log)}
*/", Encoding.UTF8));
			File.WriteAllText(@"C:\Users\Rowan\Desktop\SourceGen.log", $"===== {DateTime.Now} =====\n");
			File.AppendAllLines(@"C:\Users\Rowan\Desktop\SourceGen.log", _log);
		}

		/// <summary>
		/// Stores messages we need to log later
		/// </summary>
		// ReSharper disable once InconsistentNaming
		private static readonly List<string> _log = new();

		// ReSharper disable once ArrangeMethodOrOperatorBody
		private static void Log(string s) => _log.Add(s);

		/// <inheritdoc />
		private sealed class SyntaxReceiver : ISyntaxReceiver
		{
			/// <inheritdoc />
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				switch (syntaxNode)
				{
					//Check if we're declaring a class, record or struct
					//Essentially anything that has static and instance methods
					case ClassDeclarationSyntax cds:
					{
						Log($"Found class {cds.Identifier}");
						Log($"Modifiers are:\n'{cds.Modifiers.ToString()}'");
						// Log($"Modifiers (full) are:\n\"{cds.Modifiers.ToFullString()}\"");

						Log("");
						break;
					}
					case RecordDeclarationSyntax rds:
						break;
					case StructDeclarationSyntax sds:
						break;
				}
			}
		}
	}
}