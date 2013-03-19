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

            var sb = new StringBuilder();
            sb.AppendLine("digraph packages {");
            foreach (var package in packages)
            {
                sb.Append(" ").Append("\"").Append(package.NugetId).AppendLine("\"");
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