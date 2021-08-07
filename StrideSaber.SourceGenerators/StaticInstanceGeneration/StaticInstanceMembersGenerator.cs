using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
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
			ctx.RegisterForSyntaxNotifications(() => new SyntaxReceiver(this));
			//This is awesome by the way!!!
			// #if DEBUG
			// if (!Debugger.IsAttached) Debugger.Launch();
			// #endif
		}

	#region Diagnostic Descriptions

		// ReSharper disable InconsistentNaming

		//SIMG stands for "Static Instance Member Generator
		private static readonly DiagnosticDescriptor GenMembers_InvalidParentNode = new DiagnosticDescriptor(
				"SIMG01",
				"Invalid Parent Node",
				"Invalid parent node for attribute {0}",
				"Usage",
				DiagnosticSeverity.Error,
				true,
				"The attribute is attached to a parent node that is not valid, e.g. an assembly is being targeted by the " + nameof(GenerateStaticInstanceMembersAttribute) + ". To fix this, target a valid node instead, such as a class or struct (for the " + nameof(GenerateStaticInstanceMembersAttribute) + "."
		);

		// ReSharper restore InconsistentNaming

	#endregion

		/// <inheritdoc />
		public void Execute(GeneratorExecutionContext context)
		{
			//From my understanding, the syntax reciever is the "scan" phase that finds stuff to work on,
			//and the "execute" is where we actually do the work

			foreach (AttributeSyntax attribute in foundAttributes)
			{
				//Here we check it's attached to a class declaration
				// "attribute.Parent" is "AttributeListSyntax"
				// "attribute.Parent.Parent" is a C# fragment the attribute is applied to
				if (attribute.Parent?.Parent is not TypeDeclarationSyntax typeDeclaration)
				{
					//I'm guessing this might happen on assembly-level attributes
					SyntaxNode? invalidParentNode = attribute.Parent?.Parent ?? attribute.Parent;
					Log($"Attribute {attribute.Name} was attached to invalid node: {invalidParentNode}");
					context.ReportDiagnostic(Diagnostic.Create(GenMembers_InvalidParentNode, Location.Create(attribute.SyntaxTree, attribute.Span), attribute.Name));
					continue;
				}

				//Can't do that for interfaces (yet)
				if (attribute.Parent?.Parent is InterfaceDeclarationSyntax interfaceDeclaration)
				{
					Log($"Attribute {attribute.Name} was attached to interface node: {interfaceDeclaration.Identifier}");
					context.ReportDiagnostic(Diagnostic.Create(GenMembers_InvalidParentNode, Location.Create(attribute.SyntaxTree, attribute.Span), attribute.Name));
					continue;
				}

				Log($"Type {typeDeclaration.Identifier} has valid {nameof(GenerateStaticInstanceMembersAttribute)}");

				//Now see if we can find a child member to use as our target
				MemberDeclarationSyntax targetInstanceMember;
				//Pull out any members that have the TargetInstanceMember attribute

				var targets =
						//First check that the member declaration is either a field or a property
						typeDeclaration.Members.Where(m => m.IsKind(SyntaxKind.FieldDeclaration) || m.IsKind(SyntaxKind.PropertyDeclaration))
								//And check that the attributes it has match our target attribute
								.Where(m =>
										//Go through the lists (groups) of attributes
										m.AttributeLists.Any(
												//And see if any of the attributes in that group have a matching name
												l => l.Attributes.Any(a => GetAttributeName(a) is "TargetInstanceMemberAttribute" or "TargetInstanceMember")
										)
								)
								.ToArray();
				switch (targets.Length)
				{
					case 0:
						Log($"No members marked as target");
						break;
					case 1:
						Log($"Member {targets[0]} is target member");
						break;
					default:
					{
						Log($"Too many target members ({targets.Length})");
						for (int i = 0; i < targets.Length; i++)
						{
							MemberDeclarationSyntax memberDeclarationSyntax = targets[i];
							Log($"Member {i + 1}: {targets[0]}");
						}

						break;
					}
				}
				Log(Environment.NewLine);
			}

			//Here we write to our log file
			lock (_log)
			{
				StringBuilder sb = new($"===== {DateTime.Now} ====={Environment.NewLine}");
				for (int i = 0; i < _log.Count; i++) sb.AppendLine(_log[i]);
				_log.Clear();
				context.AddSource("SourceGenLog", SourceText.From($"/*{Environment.NewLine}{string.Join("\n\r", sb.ToString())}{Environment.NewLine}*/", Encoding.UTF8));
			}
		}

		/// <summary>
		/// A list of <see cref="GenerateStaticInstanceMembersAttribute"/> that were found. Some will be invalid, so need to check in <see cref="Execute"/>
		/// </summary>
		private readonly List<AttributeSyntax> foundAttributes = new();

	#region Logging, ignore this

		/// <summary>
		/// Stores messages we need to log later
		/// </summary>
		// ReSharper disable once InconsistentNaming
		private static readonly List<string> _log = new();

		private static void Log(string s)
		{
			lock (_log)
			{
				_log.Add(s);
			}
		}

	#endregion

		/// <summary>
		/// Gets the type name from an <see cref="AttributeSyntax"/>
		/// </summary>
		/// <param name="attribute">The attribute to get the name of</param>
		/// <remarks>
		/// The method OnVisitSyntaxNode is looking out for nodes of type AttributeSyntax only.
		/// The name of the attribute must be either EnumGenerationAttribute or its short form EnumGeneration but before the check, we extract the name of the attribute.
		/// The extraction of the name is required in case the attribute is specified with its namespace, like [DemoLibrary.EnumGeneration].
		/// In this case, we throw away the namespace.
		/// </remarks>
		private static string? GetAttributeName(AttributeSyntax attribute)
		{
			TypeSyntax type = attribute.Name;
			while (true)
				switch (type)
				{
					case IdentifierNameSyntax ins:
						return ins.Identifier.Text;
					//Move to the next section
					case QualifiedNameSyntax qns:
						type = qns.Right;
						break;
					default:
						return null;
				}
		}

		/// <inheritdoc />
		private sealed class SyntaxReceiver : ISyntaxReceiver
		{
			internal SyntaxReceiver(StaticInstanceMembersGenerator parentGenerator)
			{
				this.parentGenerator = parentGenerator;
			}

			private readonly StaticInstanceMembersGenerator parentGenerator;

			/// <inheritdoc />
			public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
			{
				//Ensure it's declaring an attribute
				if (syntaxNode is not AttributeSyntax attribute) return;
				string? name = GetAttributeName(attribute);
				//Quit if the name doesn't match
				if (name is not ("GenerateStaticInstanceMembersAttribute" or "GenerateStaticInstanceMembers")) return;
				//Add it to the list to process later
				parentGenerator.foundAttributes.Add(attribute);
			}
		}
	}
}