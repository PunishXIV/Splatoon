using System.Reflection;
using System.Runtime.Loader;
using ECommons.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;


namespace Splatoon.SplatoonScripting;



internal class Compiler
{
    internal static Assembly Load(byte[] assembly)
    {
        PluginLog.Information($"Beginning assembly load");
        if(DalamudReflector.TryGetLocalPlugin(out var instance, out var type))
        {
            var loader = type.GetField("loader", ReflectionHelper.AllFlags).GetValue(instance);
            var context = loader.GetFoP<AssemblyLoadContext>("context");
            using var stream = new MemoryStream(assembly);
            try
            {
                var a = context.LoadFromStream(stream);
                return a;
            }
            catch(Exception e)
            {
                e.LogDuo();
            }
        }
        return null;
    } 

    internal static byte[] Compile(string sourceCode, string identity)
    {
        using (var peStream = new MemoryStream())
        {
            var result = GenerateCode(sourceCode, identity).Emit(peStream);

            if (!result.Success)
            {
                PluginLog.Warning("Compilation done with error.");

                var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                foreach (var diagnostic in failures)
                {
                    PluginLog.Warning($"{diagnostic.Id}: {diagnostic.GetMessage()}");
                }

                return null;
            }

            PluginLog.Information("Compilation done without any error.");

            peStream.Seek(0, SeekOrigin.Begin);

            return peStream.ToArray();
        }
    }

    private static CSharpCompilation GenerateCode(string sourceCode, string identity = "Script")
    {
        var codeString = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.CSharp11);

        var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
        var refs = ReferenceCache.ReferenceList;
        //PluginLog.Information($"References: {references.Select(x => x.Display).Join(", ")}");

        var id = $"SplatoonScript-{identity}-{Guid.NewGuid()}";
        PluginLog.Information($"Assembly name: {id}");
        return CSharpCompilation.Create(id,
            new[] { parsedSyntaxTree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                allowUnsafe: true));
    }
}
