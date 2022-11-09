﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Microsoft.Interop
{
    public static class IncrementalGeneratorInitializationContextExtensions
    {
        public static IncrementalValueProvider<StubEnvironment> CreateStubEnvironmentProvider(this IncrementalGeneratorInitializationContext context)
        {
            return context.CompilationProvider.Select(static (comp, ct) => comp.CreateStubEnvironment());
        }

        public static void RegisterDiagnostics(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<Diagnostic> diagnostics)
        {
            context.RegisterSourceOutput(diagnostics, (context, diagnostic) =>
            {
                context.ReportDiagnostic(diagnostic);
            });
        }

        public static void RegisterConcatenatedSyntaxOutputs<TNode>(this IncrementalGeneratorInitializationContext context, IncrementalValuesProvider<TNode> nodes, string fileName)
            where TNode : SyntaxNode
        {
            IncrementalValueProvider<ImmutableArray<string>> generatedMethods = nodes
                .Select(
                    static (node, ct) => node.NormalizeWhitespace().ToFullString())
                .Collect();

            context.RegisterSourceOutput(generatedMethods,
                (context, generatedSources) =>
                {
                    // Don't generate a file if we don't have to, to avoid the extra IDE overhead once we have generated
                    // files in play.
                    if (generatedSources.IsEmpty)
                    {
                        return;
                    }

                    StringBuilder source = new();
                    // Mark in source that the file is auto-generated.
                    source.AppendLine("// <auto-generated/>");
                    foreach (string generated in generatedSources)
                    {
                        source.AppendLine(generated);
                    }

                    // Once https://github.com/dotnet/roslyn/issues/61326 is resolved, we can avoid the ToString() here.
                    context.AddSource(fileName, source.ToString());
                });
        }
    }
}
