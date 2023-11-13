using Backgrounder.Generators.Internal;

using Microsoft.CodeAnalysis;

namespace Backgrounder.Generators;

public record BackgroundContext(
    EquatableArray<Diagnostic> Diagnostics,
    BackgroundMethod Method);

public record BackgroundMethod(
    string ClassNamespace,
    string ClassName,
    string ServiceType,
    string MethodName,
    string ExtensionName,
    string MethodSignature,
    string MethodHash,
    bool IsStatic,
    bool IsAsync,
    EquatableArray<MethodParameter> Parameters);

public record MethodParameter(
    string ParameterName,
    string ParameterType);
