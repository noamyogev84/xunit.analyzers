﻿using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Xunit.Analyzers
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class ClassDataAttributeMustPointAtValidClass : XunitDiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
		   ImmutableArray.Create(Descriptors.X1007_ClassDataAttributeMustPointAtValidClass);

		internal override void AnalyzeCompilation(CompilationStartAnalysisContext context, XunitContext xunitContext)
		{
			var compilation = context.Compilation;
			var iEnumerableOfObjectArray = TypeSymbolFactory.IEnumerableOfObjectArray(compilation);

			context.RegisterSyntaxNodeAction(context =>
			{
				var attribute = (AttributeSyntax)context.Node;
				if (!(attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression is TypeOfExpressionSyntax argumentExpression))
					return;

				var semanticModel = context.SemanticModel;
				if (!Equals(semanticModel.GetTypeInfo(attribute).Type, xunitContext.Core.ClassDataAttributeType))
					return;

				var classType = (INamedTypeSymbol)semanticModel.GetTypeInfo(argumentExpression.Type).Type;
				if (classType == null || classType.Kind == SymbolKind.ErrorType)
					return;

				var missingInterface = !iEnumerableOfObjectArray.IsAssignableFrom(classType);
				var isAbstract = classType.IsAbstract;
				var noValidConstructor = !classType.InstanceConstructors.Any(c => c.Parameters.IsEmpty && c.DeclaredAccessibility == Accessibility.Public);

				if (missingInterface || isAbstract || noValidConstructor)
				{
					context.ReportDiagnostic(Diagnostic.Create(
						Descriptors.X1007_ClassDataAttributeMustPointAtValidClass,
						argumentExpression.Type.GetLocation(),
						classType.Name));
				}
			}, SyntaxKind.Attribute);
		}
	}
}
