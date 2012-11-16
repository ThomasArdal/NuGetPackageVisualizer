using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using GoCommando;
using GoCommando.Api;
using GoCommando.Attributes;
using NuGetPackageVisualizer.NuGetService;

namespace NuGetPackageVisualizer
{
    public class Program : ICommando
    {
        [NamedArgument("folder", "l")]
        [Description("Path for a folder containing packages.config file(s).")]
        [Example("-folder:\"c:\\my projects\\proj\"")]
        public string Folder { get; set; }

        [NamedArgument("recursive", "r", Default = "true")]
        [Description("If set together with folder, a recursive search for packages.config files are made from the specified folder. Default: true.")]
        [Example("-recursive:false")]
        public bool Recursive { get; set; }

        [NamedArgument("file", "f")]
        [Description("Path to a packages.config file.")]
        [Example("-file:\"c:\\my projects\\proj\\packages.config\"")]
        public string File { get; set; }

        [NamedArgument("output", "o", Default = "packages.dgml")]
        [Description("Path to the generated DGML file. Default: \"packages.dgml\".")]
        [Example("-output:.\\packages.dgml")]
        public string Output { get; set; }

        public static void Main(string[] args)
        {
            Console.WriteLine("============================");
            Console.WriteLine("= NuGet Package Visualizer =");
            Console.WriteLine("============================");
            Go.Run<Program>(args);
        }

        public void Run()
        {
            if (!Valid())
            {
                Console.WriteLine("Invoke with -? for detailed help.");
                return;
            }

            var packageFiles = GetFiles();
            var packages = GeneratePackages(packageFiles);
            GenerateFile(packages);
        }

        private IEnumerable<string> GetFiles()
        {
            var packageFiles = new List<string>();
            if (!string.IsNullOrWhiteSpace(File))
                packageFiles.Add(File);
            else
                packageFiles.AddRange(DirSearch(Folder));

            return packageFiles;
        }

        private List<PackageViewModel> GeneratePackages(IEnumerable<string> packageFiles)
        {
            var packages = new List<PackageViewModel>();
            var feedContext = new FeedContext_x0060_1(new Uri("http://nuget.org/api/v2/"));
            foreach (var file in packageFiles)
            {
                var packagesConfig = XDocument.Load(file);

                foreach (var package in packagesConfig.Descendants("package"))
                {
                    var id = package.Attribute("id").Value;
                    var version = package.Attribute("version").Value;
                    var remotePackage = feedContext.Packages.Where(x => x.Id == id && x.IsLatestVersion).FirstOrDefault();
                    if (remotePackage == null) continue;
                    if (packages.Any(p => p.NugetId == id && p.LocalVersion == version)) continue;
                    packages.Add(new PackageViewModel
                    {
                        RemoteVersion = remotePackage.Version,
                        LocalVersion = version,
                        NugetId = id,
                        Id = Guid.NewGuid().ToString(),
                        Dependencies = remotePackage.Dependencies.Split(new[] { "|" }, StringSplitOptions.RemoveEmptyEntries).Select(x =>
                            {
                                var strings = x.Split(new[] {':'});
                                return new DependencyViewModel {NugetId = strings[0], Version = strings[1]};
                            }).ToArray()
                    });
                }
            }

            return packages;
        }

        private void GenerateFile(List<PackageViewModel> packages)
        {
            XNamespace ns = "http://schemas.microsoft.com/vs/2009/dgml";

            var nodes =
                packages
                    .Select(
                        package =>
                            new XElement(ns + "Node",
                                new XAttribute("Id", package.Id),
                                new XAttribute("Label", string.Format("{0} ({1})", package.NugetId, package.LocalVersion)),
                                new XAttribute("Background", GenerateBackgroundColor(packages, package))))
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

            document.Save(Output);
        }

        private string GenerateBackgroundColor(IEnumerable<PackageViewModel> packages, PackageViewModel package)
        {
            if (package.LocalVersion != package.RemoteVersion)
            {
                return "#FF0000";
            }

            if (packages.Any(p => p.NugetId == package.NugetId && p.LocalVersion != package.LocalVersion))
            {
                return "#FCE428";
            }
            
            return "#15FF00";
        }

        private bool Valid()
        {
            if (FolderAndFileNotSpecified())
            {
                Console.WriteLine("You need to specify either folder or file.");
                return false;
            }

            if (FolderAndFileBothSpecified())
            {
                Console.WriteLine("You cannot specify both folder and file.");
                return false;
            }

            if (!string.IsNullOrWhiteSpace(File) && !System.IO.File.Exists(File))
            {
                Console.WriteLine("Could not find file: " + File);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(Folder) && !Directory.Exists(Folder))
            {
                Console.WriteLine("Could not find folder: " + Folder);
                return false;
            }
            
            return true;
        }

        private bool FolderAndFileBothSpecified()
        {
            return !string.IsNullOrWhiteSpace(Folder) && !string.IsNullOrWhiteSpace(File);
        }

        private bool FolderAndFileNotSpecified()
        {
            return string.IsNullOrWhiteSpace(Folder) && string.IsNullOrWhiteSpace(File);
        }

        private IEnumerable<string> DirSearch(string sDir)
        {
            var packageFiles = new List<string>();
            try
            {
                foreach (var d in Directory.GetDirectories(sDir))
                {
                    packageFiles.AddRange(Directory.GetFiles(d, "packages.config"));
                    if (Recursive)
                        packageFiles.AddRange(DirSearch(d));
                }
            }
            catch (Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }

            return packageFiles;
        }
    }
}
