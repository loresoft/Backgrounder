using System;
using System.Collections.Generic;
using System.Text;

using Backgrounder.Generators.Internal;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;

namespace Backgrounder.Generators;

[Generator(LanguageNames.CSharp)]
public class BackgroundGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        InitializeAttribute(context, "Backgrounder.BackgroundOperationAttribute");
        InitializeAttribute(context, "Backgrounder.BackgroundOperationAttribute`1");
    }

    private static void InitializeAttribute(IncrementalGeneratorInitializationContext context, string fullyQualifiedMetadataName)
    {
        var provider = context.SyntaxProvider.ForAttributeWithMetadataName(
            fullyQualifiedMetadataName: fullyQualifiedMetadataName,
            predicate: SyntacticPredicate,
            transform: SemanticTransform
        )
        .Where(static context => context is not null);

        // Emit the diagnostics, if needed
        var diagnostics = provider
            .Select(static (item, _) => item!.Diagnostics)
            .Where(static item => item.Count > 0);

        context.RegisterSourceOutput(diagnostics, ReportDiagnostic);

        var backgroundMethods = provider
            .Select(static (item, _) => item!.Method)
            .Where(static item => item is not null);

        context.RegisterSourceOutput(backgroundMethods, Execute);
    }

    private static void ReportDiagnostic(SourceProductionContext context, EquatableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    private static void Execute(SourceProductionContext context, BackgroundMethod backgroundMethod)
    {
        var qualifiedName = $"{backgroundMethod.ClassNamespace}.{backgroundMethod.ClassName}{backgroundMethod.MethodName}";
        var methodHash = backgroundMethod.MethodHash;

        var source = BackgroundWriter.Generate(backgroundMethod);

        context.AddSource($"{qualifiedName}Extensions.{methodHash}.g.cs", source);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is MethodDeclarationSyntax { AttributeLists.Count: > 0 } methodDeclaration
            && !methodDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword);
    }

    private static BackgroundContext? SemanticTransform(GeneratorAttributeSyntaxContext context, CancellationToken cancellationToken)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
            return null;

        var classType = methodSymbol.ContainingType;

        var classNamespace = classType.ContainingNamespace.ToDisplayString();
        var className = classType.Name;
        var methodName = methodSymbol.Name;
        var methodSignature = methodSymbol.ToDisplayString();
        var methodHash = HashCode.HashString(methodSignature).ToString("X");
        var serviceType = className;
        var extensionName = methodName;

        ExtractAttributeData(context, ref serviceType, ref extensionName);

        var parameters = new List<MethodParameter>();
        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            var parameterName = parameterSymbol.Name;
            var parameterType = parameterSymbol.Type.ToDisplayString();
            var methodParameter = new MethodParameter(parameterName, parameterType);

            parameters.Add(methodParameter);
        }

        var isAsync = HasReturnTask(methodSymbol.ReturnType);

        var backgroundMethod = new BackgroundMethod(
            ClassNamespace: classNamespace,
            ClassName: className,
            ServiceType: serviceType,
            MethodName: methodName,
            ExtensionName: extensionName,
            MethodSignature: methodSignature,
            MethodHash: methodHash,
            IsStatic: methodSymbol.IsStatic,
            IsAsync: isAsync,
            Parameters: new EquatableArray<MethodParameter>(parameters)
        );
        var diagnostics = new EquatableArray<Diagnostic>();

        return new BackgroundContext(diagnostics, backgroundMethod);
    }

    private static void ExtractAttributeData(GeneratorAttributeSyntaxContext context, ref string serviceType, ref string extensionName)
    {
        foreach (var attributeData in context.Attributes.Where(IsBackgroundOperation))
        {
            var attributeClass = attributeData.AttributeClass;
            if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length == attributeClass.TypeParameters.Length)
            {
                // if generic attribute, get service type from generic parameters
                for (var index = 0; index < attributeClass.TypeParameters.Length; index++)
                {
                    var typeParameter = attributeClass.TypeParameters[index];
                    var typeArgument = attributeClass.TypeArguments[index];

                    if (typeParameter.Name == "TService")
                        serviceType = typeArgument.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                }
            }

            foreach (var parameter in attributeData.NamedArguments)
            {
                var name = parameter.Key;
                var value = parameter.Value.Value;

                if (string.IsNullOrEmpty(name) || value == null)
                    continue;

                if (name == "ExtensionName")
                    extensionName = value.ToString();
                else if (name == "ServiceType")
                    serviceType = value.ToString();
            }
        }
    }

    private static bool HasReturnTask(ITypeSymbol returnType)
    {
        //System.Threading.Tasks.Task
        return returnType is
        {
            Name: "Task",
            ContainingNamespace:
            {
                Name: "Tasks",
                ContainingNamespace:
                {
                    Name: "Threading",
                    ContainingNamespace:
                    {
                        Name: "System"
                    }
                }
            }
        };
    }

    private static bool IsBackgroundOperation(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: "BackgroundOperationAttribute",
            ContainingNamespace.Name: "Backgrounder"
        };
    }

}
