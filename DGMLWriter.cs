using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace NuGetPackageVisualizer
{
    public class DGMLWriter : IPackageWriter
    {
        public void Write(List<PackageViewModel> packages, string file)
        {
            if (string.IsNullOrWhiteSpace(file))
            {
                file = "packages.dgml";
            }

            XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";
            var colors = new DgmlColorConfiguration();
            var nodes =
                packages
                    .Select(
                        package =>
                            new XElement(ns + "Node",
                                new XAttribute("Id", package.Id),
                                new XAttribute("Label", string.Format("{0} ({1})", package.NugetId, package.LocalVersion)),
                                new XAttribute("Background", GraphHelper.GenerateBackgroundColor(packages, package, colors))))
                    .ToList();

            var links =
                (packages
                    .SelectMany(
                        package => package.Dependencies, (package, dep) =>
                            new XElement(ns + "Link",
                                new XAttribute("Source", package.Id),
                                new XAttribute("Target", packages.Any(x => x.NugetId == dep.NugetId && x.LocalVersion == dep.Version) ? packages.First(x => x.NugetId == dep.NugetId && x.LocalVersion == dep.Version).Id : string.Format("{0} ({1})", dep.NugetId, dep.Version)))))
                .ToList();

            var document =
                new XDocument(
                    new XDeclaration("1.0", "utf-8", string.Empty),
                    new XElement(ns + "DirectedGraph", new XElement(ns + "Nodes", nodes), new XElement(ns + "Links", links)));

            document.Save(file);
        }
    }
}