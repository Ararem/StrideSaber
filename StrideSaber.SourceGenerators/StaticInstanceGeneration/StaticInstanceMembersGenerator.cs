using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
			if (!Debugger.IsAttached) Debugger.Launch();
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
			INamedTypeSymbol targetAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(TargetInstanceMemberAttribute).FullName!)!;
			INamedTypeSymbol genMembersAttributeSymbol = context.Compilation.GetTypeByMetadataName(typeof(GenerateStaticInstanceMembersAttribute).FullName!)!;
			Log($"Target Attribute Symbol is {targetAttributeSymbol}");
			Log($"GenMembers Attribute Symbol is {genMembersAttributeSymbol}");
			foreach (var type in receiver!.Types) ProcessType(type, context, sb, genMembersAttributeSymbol, targetAttributeSymbol!);

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
		private static void ProcessType(TypeToProcess toProcess, GeneratorExecutionContext context, in StringBuilder sb, INamedTypeSymbol genMembersAttributeSymbol, INamedTypeSymbol targetAttributeSymbol)
		{
			INamedTypeSymbol type = toProcess.Type;
			sb.Clear();
			Log($"\nExamining type {type}");

			//Check if it is actually something we should generate
			//So if we don't have any attributes that match our 'generate members on this' attribute we return
			if (!type.GetAttributes().Any(a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, genMembersAttributeSymbol)))
			{
				Log($"Class not marked for generation, skipping");
				return;
			}

			Log("Type marked for generation, processing");

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
			Log($"\tScanning {members.Length} class members");
			foreach (ISymbol member in members)
			{
				Log($"\tMember {member}:");
				//If the member is not a field or property we skip it
				if (member is not IFieldSymbol or IPropertySymbol)
				{
					Log($"\tMember is not field or property ({member.Kind})");
					continue;
				}

				//Now check if we have a target attribute on that field/property
				bool isTarget = member.GetAttributes().Any(
						//We check if the attribute is the same as our attribute we use to mark our target members
						a => SymbolEqualityComparer.Default.Equals(a.AttributeClass, targetAttributeSymbol)
				);
				//Member isn't a target, skip
				if (!isTarget)
				{
					Log("\tMember not marked as target");
					continue;
				}

				//Have to validate it's static or else we can't access it
				if (!member.IsStatic)
				{
					Log("Member is not static, ignoring and warning");
					ReportDiag(TargetMustBeStatic, member);
					continue;
				}

				//Same thing for visibility
				if (member.DeclaredAccessibility is not Accessibility.Internal or Accessibility.Public or Accessibility.ProtectedOrInternal)
				{
					Log("Member is not static, ignoring and warning");
					ReportDiag(TargetMustBeStatic, member);
					continue;
				}

				//Member is marked as target
				//Now we have to make sure we only have at most 1 instance member
				if (targetMember is null)
				{
					Log($"Target member found ({member})");
					targetMember = member;
				}
				else
				{
					Log($"Target member duplicate found ({member})");
					ReportDiag(TooManyFieldTargets, member);
				}
			}

			Log("\tFinished scanning members");
			Log("\tGenerating members");
			//TODO: Handle if it's in the global namespace
			sb.Append($@"
//Auto generated by a roslyn source generator 
namespace {type.ContainingNamespace}
{{
	partial class {type.Name}
	{{");
			const string instanceName = "__instance";
			Log($"\t\tTarget instance not found, generating as '{instanceName}'");
			sb.Append($@"
		/// <summary>
		///  A roslyn source-generator generated instance that will be used as the target for static instance members
		/// </summary>
		[System.Runtime.CompilerServices.CompilerGenerated]
		private static readonly {type.Name} {instanceName} = new();");

			//Now we generate a static version of each instance member
			//Only generate public instance members
			foreach (var member in type.GetMembers().Where(m => !m.IsStatic && (m.DeclaredAccessibility == Accessibility.Public)))
			{
				switch (member)
				{
					case IFieldSymbol field:
						sb.Append($@"
		{field.GetDocumentationCommentXml()}
		public static {field.Type.ContainingNamespace}.{field.Type.Name} {field.Name}
		{{
			get => {instanceName}.{field.Name};
			set => {instanceName}.{field.Name} = value;
		}}");
						break;

					case IPropertySymbol prop:
						sb.Append($@"
		{prop.GetDocumentationCommentXml()}
		public static {prop.Type.ContainingNamespace}.{prop.Type.Name} {prop.Name}
		{{
			get => {instanceName}.{prop.Name};
			set => {instanceName}.{prop.Name} = value;
		}}");
						break;
					case IMethodSymbol {MethodKind: MethodKind.Ordinary} method:
						//Build the return type strings
						string returnType = "";
						if (method.IsAsync) returnType += "async";
						if (method.ReturnsByRefReadonly) returnType += "ref readonly";
						else if (method.ReturnsByRef) returnType += "ref";
						returnType += $"{method.ReturnType.ContainingNamespace}.{method.ReturnType}";
						if (method.ReturnsVoid) returnType = "void";
						//Now build the parameters
						//methodCallArgs is when we actually call the method: `foo(x,y,z)`
						//methodDecArgs is when we declare the method: `foo(int x, int z, bar z)`
						string methodCallArgs = "", methodDecArgs = "";
						for (int i = 0; i < method.Parameters.Length; i++)
						{
							IParameterSymbol param = method.Parameters[i];
							//Append the types and names of the parameters
							methodCallArgs += param.Name;
							if (!param.Type.ContainingNamespace.IsGlobalNamespace)
								methodDecArgs += param.Type.ContainingNamespace + ".";
							methodDecArgs += param.Type.Name;

							//Only add commas on iterations that aren't the last
							if (i != method.Parameters.Length - 1)
							{
								methodCallArgs += ", ";
								methodDecArgs += ", ";
							}
						}

						sb.Append($@"
		{method.GetDocumentationCommentXml()}
		public static {returnType} {method.Name}({methodDecArgs}) => {instanceName}.{method.Name}({methodCallArgs});");
						break;
				}
			}

			sb.Append($@"
	}} //Class
}} //Namespace
");
			context.AddSource(type.Name, sb.ToString());

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

		private static readonly DiagnosticDescriptor TargetMustBeStatic = new(
						"SIMG04",
						"Target must be static",
						"The target instance member must be static",
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