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

			//Here we write to our log file
			lock (_log)
			{
				StringBuilder sb = new($"===== {DateTime.Now} ====={Environment.NewLine}");
				for (int i = 0; i < _log.Count; i++) sb.AppendLine(_log[i]);
				_log.Clear();
				ctx.AddSource("SourceGenLog", SourceText.From($"/*{Environment.NewLine}{string.Join("\n\r", sb.ToString())}{Environment.NewLine}*/", Encoding.UTF8));
			}
		}

		/// <summary>
		/// Stores messages we need to log later
		/// </summary>
		// ReSharper disable once InconsistentNaming
		private static readonly List<string> _log = new();

		private static void Log(string s)
		{
			lock (_log)
				_log.Add(s);
		}

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
						var attributes = cds.AttributeLists //The lists of all the attribute declaration lists: {[Attribute_1], [Attribute_2]}
								.Select(attributeLists => attributeLists.Attributes) //The separate attributes: {Attribute_1, Attribute_2}
								.SelectMany(syntaxList => syntaxList) //?? Idk what this is now lol
								.Select(a => a.Name);
						Log($"Attributes are {string.Join(", ", attributes)}");
						Log($"Modifiers are: '{cds.Modifiers.ToString()}'");
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