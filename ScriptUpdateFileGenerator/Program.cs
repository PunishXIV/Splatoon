using System.Text.RegularExpressions;

namespace ScriptUpdateFileGenerator;

internal partial class Program
{
    static List<string> Content = [];

    static void Main(string[] args)
    {
        if(args.Length != 2)
        {
            Console.WriteLine("Input and output destinations must be defined");
            Environment.Exit(0);
        }
        ProcessDirectory([""], args[0]);
        File.WriteAllText(args[1], string.Join("\n",Content));
    }

    static void ProcessDirectory(string[] path, string directory)
    {
        foreach (var file in Directory.GetFiles(directory, "*.cs"))
        {
            try
            {
                var fname = Path.GetFileName(file);
                var virtualPath = $"{string.Join("/", path)}/{fname}";
                Console.WriteLine($"Processing file {file} ({virtualPath})");
                var content = File.ReadAllText(file);
                var namespac = ExtractNamespaceFromCode(content);
                var clas = ExtractClassFromCode(content);
                var version = ExtractVersionFromCode(content);
                Console.WriteLine($"  Namespace: {namespac}, Class: {clas}, Version: {version}");
                if (namespac != null && clas != null && version != null)
                {
                    var line = $"{namespac}@{clas},{version},https://github.com/PunishXIV/Splatoon/raw/main/SplatoonScripts{virtualPath}";
                    Console.WriteLine($"  {line}");
                    Content.Add(line);
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }
        foreach(var dir in Directory.GetDirectories(directory))
        {
            ProcessDirectory([..path, Path.GetFileName(dir)], dir);
        }
    }

    internal static string? ExtractNamespaceFromCode(string code)
    {
        var regex = NamespaceRegex();
        var matches = regex.Match(code);
        if (matches.Success && matches.Groups.Count > 1)
        {
            return matches.Groups[1].Value;
        }
        return null;
    }

    static string? ExtractClassFromCode(string code)
    {
        var regex = ClassRegex();
        var matches = regex.Match(code);
        if (matches.Success && matches.Groups.Count > 1)
        {
            return matches.Groups[1].Value;
        }
        return null;
    }

    static int? ExtractVersionFromCode(string code)
    {
        var regex = VersionRegex();
        var matches = regex.Match(code);
        if (matches.Success && matches.Groups.Count > 1)
        {
            return int.Parse(matches.Groups[1].Value);
        }
        return null;
    }

    [GeneratedRegex("namespace[\\s]+([a-z0-9_\\.]+)", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex NamespaceRegex();

    [GeneratedRegex("([a-z0-9_\\.]+)\\s*:\\s*SplatoonScript", RegexOptions.IgnoreCase, "en-US")]
    private static partial Regex ClassRegex();
    [GeneratedRegex("override.+Metadata.+Metadata.+new\D+([0-9]+)")]
    private static partial Regex VersionRegex();
}
