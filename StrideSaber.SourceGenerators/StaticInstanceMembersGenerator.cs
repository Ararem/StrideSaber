using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.IO;

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

			File.WriteAllText(@"C:\Users\Rowan\Desktop\SourceGen.log", $"====={DateTime.Now}=====\n");
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

						Log(cds.Modifiers.Any(SyntaxKind.StaticKeyword) ? "Class is static" : "Class is not static");

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