using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NuGetPackageVisualizer
{
    public class GraphvizWriter : IPackageWriter
    {
        public void Write(List<PackageViewModel> packages, string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                file = "packages.dot";
            }
            
            var colors = new GraphVizColorConfiguration();
            var sb = new StringBuilder();
            
            WriteHeader(sb);
            foreach (var package in packages)
            {                                
                sb.AppendFormat(" \"{0}\"[fillcolor=\"{1}\",label=\"{2}\"];", 
                    package.GraphId(),
                    GraphHelper.GenerateBackgroundColor(packages, package, colors),
                    package.DisplayVersion()).AppendLine();

                var dependenciesToWrite = package.Dependencies.Select(dep =>
                    String.Format(" \"{0}\" -> \"{1}\";", package.GraphId(), DependencyNodeId(dep, packages))).ToArray();
                sb.AppendLine(String.Join(Environment.NewLine, dependenciesToWrite));                
            }
            WriteClose(sb);

            File.WriteAllText(file, sb.ToString());
        }

        private static void WriteHeader(StringBuilder sb)
        {
            sb.AppendLine("digraph packages {");
            sb.AppendLine(" node [shape=box, style=\"rounded,filled\"];");
        }

        private static void WriteClose(StringBuilder sb)
        {
            sb.AppendLine("}");
        }

        private static string DependencyNodeId(DependencyViewModel dep, IEnumerable<PackageViewModel> packages)
        {
            string targetId = dep.GraphId();
            // If version on dep is not explicitly stated, we should use an existing package with same nuget id.
            // This greatly minimizes the number of disconnected nodes.
            if (string.IsNullOrWhiteSpace(dep.Version))
            {
                PackageViewModel existingModel = packages.FirstOrDefault(x => x.NugetId == dep.NugetId);
                if (existingModel != null)
                    targetId = existingModel.GraphId();
            }
            return targetId;
        }
    }
}