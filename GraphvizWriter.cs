using System.Collections.Generic;
using System.IO;
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
            sb.AppendLine("digraph packages {");
            sb.AppendLine(" node [shape=box, style=\"rounded,filled\"]");
            foreach (var package in packages)
            {                                
                sb.Append(" ").AppendFormat("\"{0}\"[fillcolor=\"{1}\"]", 
                    package.NugetId,
                    GraphHelper.GenerateBackgroundColor(packages, package, colors)).AppendLine();

                foreach (var dep in package.Dependencies)
                {
                    sb.Append(" ")
                      .Append("\"")
                      .Append(package.NugetId)
                      .Append("\" -> \"")
                      .Append(dep.NugetId)
                      .AppendLine("\"");
                }
            }
            sb.AppendLine("}");

            File.WriteAllText(file, sb.ToString());
        }
    }
}