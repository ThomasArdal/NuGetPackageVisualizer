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

        [NamedArgument("outputtype", "ot", Default = "dgml")]
        [Description("Sets the type of the output file. The following file types are supported: dgml, graphviz.")]
        [Example("-outputtype:dgml")]
        public string OutputType { get; set; }

        [NamedArgument("output", "o")]
        [Description("Path to the generated DGML file. Default: \"packages.dgml\" or \"packages.dot\".")]
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
            switch (OutputType)
            {
                case "dgml":
                    new DGMLWriter().Write(packages, Output);
                    break;
                case "graphviz":
                    new GraphvizWriter().Write(packages, Output);
                    break;
            }
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
