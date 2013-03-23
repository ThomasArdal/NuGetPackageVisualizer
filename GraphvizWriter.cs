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
            sb.AppendLine("digraph packages {");
            sb.AppendLine(" node [shape=box, style=\"rounded,filled\"]");

            var packageLookup = packages.ToDictionary(x => x.GraphId());

            foreach (var package in packages)
            {                                
                sb.AppendFormat(" \"{0}\"[fillcolor=\"{1}\",label=\"{2}\"]", 
                    package.GraphId(),
                    GraphHelper.GenerateBackgroundColor(packages, package, colors),
                    package.DisplayVersion()).AppendLine();

                foreach (var dep in package.Dependencies.Where(d => !packageLookup.ContainsKey(d.GraphId())))
                {
                    sb.AppendFormat("\"{0}\"[label=\"{1}\"]", dep.GraphId(), dep.DisplayVersion());                    
                }

                foreach (var dep in package.Dependencies)
                {
                    sb.Append(" ")
                      .Append("\"")
                      .Append(package.GraphId())
                      .Append("\" -> \"")
                      .Append(dep.GraphId())
                      .AppendLine("\"");
                }
            }
            sb.AppendLine("}");

            File.WriteAllText(file, sb.ToString());
        }
    }
}