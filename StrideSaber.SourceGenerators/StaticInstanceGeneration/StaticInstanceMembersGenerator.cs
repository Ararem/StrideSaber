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
		private static readonly DiagnosticDescriptor GenMembers_InvalidParentNode = new(
						"SIMG01",
						"Invalid Parent Node",
						"Invalid parent node for attribute {0}",
						"Usage",
						DiagnosticSeverity.Error,
						true,
						"The attribute is attached to a parent node that is not valid, e.g. an assembly is being targeted by the " + nameof(GenerateStaticInstanceMembersAttribute) + ". To fix this, target a valid node instead, such as a class or struct (for the " + nameof(GenerateStaticInstanceMembersAttribute) + "."
				);

		private static readonly DiagnosticDescriptor GenMembers_TooManyFieldTargets = new(
						"SIMG02",
						"Too many target declarations",
						"Too many fields declared as target instance member",
						"Usage",
						DiagnosticSeverity.Warning,
						true,
						"There are too many fields in a multi-variable initializer expression that has the attribute " + nameof(TargetInstanceMemberAttribute) + ". Try splitting it into a single variable declaration with, and only mark one field. The selected variable is undefined, but will likely be the first variable declared (this is undefined, do not presume it will always be true)."
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

				//Now see if we can find a child field to use as our target
				SyntaxToken? targetInstanceMember;
				//Pull out any members that have the TargetInstanceMember attribute
				var targetTokens =
						//First check that the member declaration is either a field or a property
						typeDeclaration.Members.OfType<FieldDeclarationSyntax>()
								//And check that the attributes it has match our target attribute
								.Where(m =>
										//Go through the lists (groups) of attributes
										m.AttributeLists.Any(
												//And see if any of the attributes in that group have a matching name
												l => l.Attributes.Any(a => GetAttributeName(a) is "TargetInstanceMemberAttribute" or "TargetInstanceMember")
										)
								)
								//Now we extract the SyntaxToken (aka the identifier/variable name) from the fields
								.SelectMany(f => f.Declaration.Variables)
								.Select(v => v.Identifier)
								.ToArray();
				switch (targetTokens.Length)
				{
					case 0:
						Log("No members marked as target");
						targetInstanceMember = null;
						break;
					//Like explained in the `default` section, fields can actually have several fields declared in one section
					//So we skip to special handling if there's more than one
					case 1 when targetTokens[0] is {Declaration: {Variables: {Count: >1}}}:
						goto default;
					case 1:
						Log($"Member {targetTokens[0]} is target member");
						targetInstanceMember = targetTokens[0].Declaration.Variables[0].Identifier;
						break;
					default:
					{
						Log($"Too many target members ({targetTokens.Length}):");

						for (int i = 0; i < targetTokens.Length; i++)
						{
							FieldDeclarationSyntax? field = targetTokens[i];
							//You can declare multiple vars in a single line
							//E.g.`int x,y,z`
							var vars = field.Declaration.Variables;
							//Only 1 declared, which is good
							if (vars.Count == 1)
							{
								targetInstanceMember = vars[0].Identifier;
							}
							else
							{
							}

							Log($"Member {i + 1}: {(targetTokens[i] as PropertyDeclarationSyntax)?.Declaration}");
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