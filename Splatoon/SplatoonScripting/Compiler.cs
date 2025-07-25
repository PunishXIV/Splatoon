﻿using ECommons.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Reflection;
using System.Runtime.Loader;


namespace Splatoon.SplatoonScripting;



internal class Compiler
{
    internal static Assembly Load(byte[] assembly, byte[] pdb)
    {
        PluginLog.Debug($"Beginning assembly load");
        if(DalamudReflector.TryGetLocalPlugin(out var instance, out var context, out var type))
        {
            using var stream = new MemoryStream(assembly);
            using var streamPdb = new MemoryStream(pdb);
            try
            {
                var a = context.LoadFromStream(stream, streamPdb);
                return a;
            }
            catch(Exception e)
            {
                e.LogDuo();
            }
        }
        return null;
    }

    internal static (byte[] Assembly, byte[] Pdb)? Compile(string sourceCode, string identity, string path = null)
    {
        using var peStream = new MemoryStream();
        using var pdbStream = new MemoryStream();
        var code = GenerateCode(sourceCode, identity);
        var result = code.Emit(peStream, pdbStream);

        if(!result.Success)
        {
            var ns = ScriptingProcessor.ExtractNamespaceFromCode(sourceCode);
            var cls = ScriptingProcessor.ExtractClassFromCode(sourceCode);
            //var updatePath = $"https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts/{ns.ReplaceFirst("SplatoonScriptsOfficial.","").Replace("_", " ").Replace(".", "/")}/{cls.Replace("_", " ")}.cs";
            var updateName = $"{ns}@{cls}";
            PluginLog.Warning($"Compilation done with error ({identity}, {updateName})");

            if(ScriptingProcessor.ForceUpdate != null)
            {
                ScriptingProcessor.ForceUpdate.Add(updateName);
                PluginLog.Warning($"An attempt to update {updateName} will be made if it will be found in the update list");
            }
            var failures = result.Diagnostics.Where(diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

            foreach(var diagnostic in failures)
            {
                PluginLog.Warning($"{diagnostic.Id}: {diagnostic.GetMessage()}");
            }
            Svc.Framework.RunOnFrameworkThread(() =>
            {
                P.ScriptUpdateWindow.FailedScripts_Add(path);
            }).Wait();

            return null;
        }

        PluginLog.Debug("Compilation done without any error.");

        peStream.Seek(0, SeekOrigin.Begin);
        pdbStream.Seek(0, SeekOrigin.Begin);

        return (peStream.ToArray(), pdbStream.ToArray());
    }

    private static CSharpCompilation GenerateCode(string sourceCode, string identity = "Script")
    {
        var codeString = SourceText.From(sourceCode);
        var options = CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview);

        var parsedSyntaxTree = SyntaxFactory.ParseSyntaxTree(codeString, options);
        var refs = ReferenceCache.ReferenceList;
        //PluginLog.Information($"References: {references.Select(x => x.Display).Join(", ")}");

        var id = $"SplatoonScript-{identity}-{Guid.NewGuid()}";
        PluginLog.Debug($"Assembly name: {id}");
        return CSharpCompilation.Create(id,
            new[] { parsedSyntaxTree },
            references: refs,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default,
                allowUnsafe: true));
    }
}
