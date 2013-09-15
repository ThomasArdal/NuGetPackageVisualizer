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

        [NamedArgument("output", "o", Default = "packages")]
        [Description("The name of the generated file for the whole repository. Default: \"packages\".")]
        [Example("-output:.\\packages")]
        public string Output { get; set; }

        [NamedArgument("repositoryuri", "ru", Default = @"http://nuget.org/api/v2/")]
        [Description("The URI of the NuGet repository to use for reference. Default: \"http://nuget.org/api/v2/\".")]
        [Example("-repositoryuri:\"http://nuget.org/api/v2/\"")]
        public string RepositoryUrl { get; set; }

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
            var packages = new List<PackageViewModel>();
            foreach (var packageFile in packageFiles)
            {
                if (!(Path.GetDirectoryName(packageFile).EndsWith(".nuget")))
                {
                    var projectPackages = this.GeneratePackages(packageFile);
                    foreach (var package in projectPackages)
                    {
                        if (package.LocalVersion == "" || packages.Any(p => p.NugetId == package.NugetId && p.LocalVersion == package.LocalVersion)) continue;
                        packages.Add(package);
                    }
                    this.GenerateFile(projectPackages, BuildFilePath(Path.GetFileName(Path.GetDirectoryName(packageFile))));
                }

            }
            GenerateFile(packages, BuildFilePath(Output));
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

        private List<PackageViewModel> GeneratePackages(string File)
        {
            var packages = new List<PackageViewModel>();
            var feedContext = new FeedContext_x0060_1(new Uri(RepositoryUrl))
                {
                    IgnoreMissingProperties = true
                };

            var packagesConfig = XDocument.Load(File);
                var dependencies = new List<DependencyViewModel>();

                foreach (var package in packagesConfig.Descendants("package"))
                {
                    var id = package.Attribute("id").Value;
                    var version = package.Attribute("version").Value;
                    var remotePackage = feedContext.Packages.OrderByDescending(x => x.Version).Where(x => x.Id == id && x.IsLatestVersion && !x.IsPrerelease).FirstOrDefault();

                    dependencies.Add(new DependencyViewModel { NugetId = id, Version = version });
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

                    foreach (var dependency in packages.Last().Dependencies)
                    {
                        var pack = dependencies.FirstOrDefault(x => x.NugetId == dependency.NugetId && x.Version == dependency.Version);
                        if (pack == null) continue;
                        dependencies.Remove(pack);
                    }

                }
                packages.Add(
                    new PackageViewModel
                    {
                        RemoteVersion = "",
                        LocalVersion = "",
                        NugetId = Path.GetFileName(Path.GetDirectoryName(File)),
                        Id = Guid.NewGuid().ToString(),
                        Dependencies = dependencies.ToArray()
                    });

            return packages;
        }

        private void GenerateFile(List<PackageViewModel> packages,string fileName)
        {
            switch (OutputType)
            {
                case "dgml":
                    new DGMLWriter().Write(packages, fileName);
                    break;
                case "graphviz":
                    new GraphvizWriter().Write(packages, fileName);
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

        private string BuildFilePath(string name)
        {
            
            return string.Format("{0}.{1}",name, GetFileExtension());
        }

        private string GetFileExtension()
        {
            if (OutputType == "graphviz") return "dot";
            return OutputType;

        }
    }
}
