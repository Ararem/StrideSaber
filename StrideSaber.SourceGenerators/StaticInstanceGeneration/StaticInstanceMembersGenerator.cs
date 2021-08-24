using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace StrideSaber.SourceGenerators.StaticInstanceGeneration
{
	/// <inheritdoc/>
	[Generator]
	public sealed class StaticInstanceMembersGenerator : ISourceGenerator
	{
		/// <inheritdoc/>
		public void Initialize(GeneratorInitializationContext ctx)
		{
			// Register a factory that can create our custom syntax receiver
			ctx.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
			//This is awesome by the way!!!
			// #if DEBUG
			// if (!Debugger.IsAttached) Debugger.Launch();
			// #endif
		}

		//From my understanding, the syntax receiver is the "scan" phase that finds stuff to work on,
		//and the "execute" is where we actually do the work
		/// <inheritdoc/>
		public void Execute(GeneratorExecutionContext context)
		{
			SyntaxReceiver receiver = (SyntaxReceiver) context.SyntaxContextReceiver!;

			//Create a StringBuilder to reuse
			StringBuilder sb = new(16384);
			INamedTypeSymbol targetAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(GenerateStaticInstanceMembersAttribute).AssemblyQualifiedName!)!;
			Log($"Target Attribute Symbol is {targetAttributeSymbol}");
			foreach (var type in receiver!.Types) ProcessType(type, context, sb, targetAttributeSymbol);

			//Here we write to our log file
			lock (_log)
			{
				StringBuilder logBuilder = new($"===== {DateTime.Now} ====={Environment.NewLine}");
				for (int i = 0; i < _log.Count; i++) logBuilder.AppendLine(_log[i]);
				_log.Clear();
				context.AddSource("SourceGenLog", SourceText.From($"/*{Environment.NewLine}{logBuilder}{Environment.NewLine}*/", Encoding.UTF8));
			}
		}

		/// <summary>
		/// Processes a given <see cref="ITypeSymbol"/>, generating static instance members for it
		/// </summary>
		/// <param name="toProcess"></param>
		/// <param name="context"></param>
		/// <param name="sb"></param>
		/// <param name="targetAttributeSymbol"></param>
		private static void ProcessType(TypeToProcess toProcess, GeneratorExecutionContext context, in StringBuilder sb, INamedTypeSymbol targetAttributeSymbol)
		{
			INamedTypeSymbol type = toProcess.Type;
			Log($"\nProcessing type {type}");

		#region Type Validation

			//Ensure the class is top-level (not a nested class)
			if (!SymbolEqualityComparer.Default.Equals(type.ContainingSymbol, type.ContainingNamespace))
			{
				Log($"Type is not top level (Nested in {type.ContainingSymbol})");
				ReportDiag(ClassMustBeTopLevel, type);
				return;
			}

			//Also check for static-ness
			if (type.IsStatic)
			{
				Log("Type is static");
				ReportDiag(ClassIsStatic, type);
				return;
			}

			//Loop over all the members declared in the class
			var members = type.GetMembers();
			ISymbol? targetMember = null;
			Log("Scanning class members");
			foreach (ISymbol member in members)
			{
				//If the member is not a field or property we skip it
				if (member is not IFieldSymbol or IPropertySymbol) continue;

				//Now check if we have a target attribute on that field/property
				bool isTarget = member.GetAttributes().Any(
						//We check if the attribute is the same as our attribute we use to mark our target members
						a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, targetAttributeSymbol)
				);
				//Member isn't a target, skip
				if (!isTarget) continue;

				//Member is marked as target
				//Now we have to make sure we only have at most 1 instance member
				if (targetMember is null)
				{
					targetMember = member;
					Log($"Target member found ({targetMember})");
				}
				else
				{
					Log($"Target member duplicate found ({member})");
					ReportDiag(TooManyFieldTargets, member);
				}
			}

		#endregion

			sb.Clear();

			void ReportDiag(DiagnosticDescriptor desc, ISymbol target)
			{
				//Gotta loop through all the locations the class was declared (partial classes)
				foreach (var loc in target.Locations)
					context.ReportDiagnostic(Diagnostic.Create(desc, loc));
			}
		}

		//TODO: add target member support
		private sealed record TypeToProcess(INamedTypeSymbol Type);

		/// <inheritdoc/>
		private sealed class SyntaxReceiver : ISyntaxContextReceiver
		{
			public readonly List<TypeToProcess> Types = new();

			/// <summary>
			///  Called for every syntax node in the compilation, we can inspect the nodes and save any information useful for generation
			/// </summary>
			public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
			{
				if (context.Node is TypeDeclarationSyntax typeDec and not InterfaceDeclarationSyntax)
				{
					//Get the symbol being declared by the type
					INamedTypeSymbol type = context.SemanticModel.GetDeclaredSymbol(typeDec)!;
					Types.Add(new TypeToProcess(type!));
				}
			}
		}

	#region Diagnostic Descriptions

		// ReSharper disable StringLiteralTypo

		//SIMG stands for "Static Instance Member Generator
		private static readonly DiagnosticDescriptor ClassMustBeTopLevel = new(
						"SIMG01",
						"Class must be top level",
						"The target class must not be a nested class (inside another class). It must be a class directly inside a namespace.",
						"Usage",
						DiagnosticSeverity.Error,
						true
				);

		private static readonly DiagnosticDescriptor TooManyFieldTargets = new(
						"SIMG02",
						"Too many target declarations",
						"Too many fields declared as target instance member",
						"Usage",
						DiagnosticSeverity.Warning,
						true,
						"There are too many fields in a multi-variable initializer expression that has the attribute " + nameof(TargetInstanceMemberAttribute) + ". Try splitting it into a single variable declaration with, and only mark one field. The selected variable is undefined, but will likely be the first variable declared (this is undefined, do not presume it will always be true)."
				);

		private static readonly DiagnosticDescriptor ClassIsStatic = new(
						"SIMG03",
						"Class cannot be static",
						"The target class must not be static",
						"Usage",
						DiagnosticSeverity.Error,
						true
				);

		// ReSharper restore StringLiteralTypo

	#endregion

	#region Logging, ignore this

		/// <summary>
		///  Stores messages we need to log later
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
	}
}