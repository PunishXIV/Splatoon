using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Splatoon.SplatoonScripting
{
    internal static class ReferenceCache
    {
        static ImmutableList<MetadataReference> referenceList = null;
        internal static ImmutableList<MetadataReference> ReferenceList 
        { 
            get
            {
                referenceList ??= BuildReferenceList();
                return referenceList;
            } 
        }

        static ImmutableList<MetadataReference> BuildReferenceList()
        {
            PluginLog.Debug("Rebuilding references");
            var references = new List<MetadataReference>();
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location), "*", SearchOption.TopDirectoryOnly))
            {
                if (IsValidAssembly(f))
                {
                    PluginLog.Debug($"Adding reference: {f}");
                    references.Add(MetadataReference.CreateFromFile(f));
                }
            }
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(typeof(System.Windows.Forms.Form).Assembly.Location), "*", SearchOption.TopDirectoryOnly))
            {
                if (IsValidAssembly(f))
                {
                    PluginLog.Debug($"Adding reference: {f}");
                    references.Add(MetadataReference.CreateFromFile(f));
                }
            }
            foreach (var f in Directory.GetFiles(Svc.PluginInterface.AssemblyLocation.DirectoryName, "*", SearchOption.AllDirectories))
            {
                if (IsValidAssembly(f))
                {
                    PluginLog.Debug($"Adding reference: {f}");
                    references.Add(MetadataReference.CreateFromFile(f));
                }
            }
            foreach (var f in Directory.GetFiles(Path.GetDirectoryName(Svc.PluginInterface.GetType().Assembly.Location), "*", SearchOption.AllDirectories))
            {
                if (IsValidAssembly(f))
                {
                    PluginLog.Debug($"Adding reference: {f}");
                    references.Add(MetadataReference.CreateFromFile(f));
                }
            }
            return references.ToImmutableList();
        }

        static bool IsValidAssembly(string path)
        {
            try
            {
                var assembly = AssemblyName.GetAssemblyName(path);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }
    }
}
