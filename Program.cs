using GoCommando;
using GoCommando.Api;
using GoCommando.Attributes;
using NuGetPackageVisualizer.NuGetService;
using System;
using System.Collections.Generic;
using System.Data.Services.Client;
using System.IO;
using System.Linq;
using System.Net;
using System.Xml.Linq;

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

        [NamedArgument("outputpath", "op", Default = "")]
        [Description("Sets the root path where output files should be written. Default: \"\" the folder where NuGet Package Visualizer was run.")]
        [Example("-outputpath:\"C:\\temp\"")]
        public string OutputPath { get; set; }

        [NamedArgument("output", "o", Default = "packages")]
        [Description("The name of the generated file for the whole source folder. Default: \"packages\".")]
        [Example("-output:.\\packages")]
        public string Output { get; set; }

        [NamedArgument("repositoryuri", "ru", Default = @"http://nuget.org/api/v2/")]
        [Description("The URI of the NuGet repository to use for reference. Default: \"http://nuget.org/api/v2/\".")]
        [Example("-repositoryuri:\"http://nuget.org/api/v2/\"")]
        public string RepositoryUrl { get; set; }

        [NamedArgument("wholediagram", "wd", Default = "true")]
        [Description("Whether to generate a diagram for the whole source. Default: true.")]
        [Example("-wholediagram:true")]
        public bool WholeDiagram { get; set; }

        [NamedArgument("projectdiagrams", "pd", Default = "false")]
        [Description("Whether to generate diagram per project in the source folder. Default: false.")]
        [Example("-projectdiagrams:false")]
        public bool ProjectDiagrams { get; set; }

        [NamedArgument("username", "u", Default = "")]
        [Description("The user name for accessing a protected package feed. Default: \"\" (empty).")]
        [Example("-username:jdoe")]
        public string UserName { get; set; }

        [NamedArgument("password", "pw", Default = "")]
        [Description("The password for accessing a protected package feed. Default: \"\" (empty).")]
        [Example("-password:XYZ")]
        public string Password { get; set; }

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
                if (Path.GetDirectoryName(packageFile).EndsWith(".nuget")) continue;

                var projectPackages = GeneratePackages(packageFile);
                foreach (
                    var package in
                        projectPackages.Where(
                            package =>
                                package.LocalVersion != "" &&
                                !packages.Any(p => p.NugetId == package.NugetId && p.LocalVersion == package.LocalVersion)))
                {
                    packages.Add(package);
                }

                if (ProjectDiagrams) GenerateFile(projectPackages, BuildFilePath(Path.GetFileName(Path.GetDirectoryName(packageFile))));
            }

            if (WholeDiagram) GenerateFile(packages, BuildFilePath(Output));
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

        private List<PackageViewModel> GeneratePackages(string file)
        {
            var packages = new List<PackageViewModel>();
            var feedContext = new FeedContext_x0060_1(new Uri(RepositoryUrl))
                {
                    IgnoreMissingProperties = true
                };

            ApplyCredentials(feedContext);

            var packagesConfig = XDocument.Load(file);
            var dependencies = new List<DependencyViewModel>();

            foreach (var package in packagesConfig.Descendants("package"))
            {
                var id = package.Attribute("id").Value;
                var version = package.Attribute("version").Value;
                // ReSharper disable ReplaceWithSingleCallToFirstOrDefault
                var remotePackage =
                    feedContext
                        .Packages
                        .OrderByDescending(x => x.Version)
                        .Where(x => x.Id == id && x.IsLatestVersion && !x.IsPrerelease)
                        .FirstOrDefault();
                // ReSharper restore ReplaceWithSingleCallToFirstOrDefault

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
                            var strings = x.Split(new[] { ':' });
                            return new DependencyViewModel { NugetId = strings[0], Version = strings[1] };
                        }).ToArray()
                });

                foreach (
                    var pack in
                        packages
                            .Last()
                            .Dependencies
                            .Select(dependency =>
                                dependencies
                                    .FirstOrDefault(x => x.NugetId == dependency.NugetId && x.Version == dependency.Version))
                            .Where(pack => pack != null))
                {
                    dependencies.Remove(pack);
                }
            }
            packages.Add(
                new PackageViewModel
                {
                    RemoteVersion = "",
                    LocalVersion = "",
                    NugetId = Path.GetFileName(Path.GetDirectoryName(file)),
                    Id = Guid.NewGuid().ToString(),
                    Dependencies = dependencies.ToArray()
                });

            return packages;
        }

        private void ApplyCredentials(DataServiceContext dataServiceContext)
        {
            if (!string.IsNullOrEmpty(UserName))
            {
                if (string.IsNullOrEmpty(Password))
                {
                    promptForPassword();
                }
                dataServiceContext.Credentials = new NetworkCredential(UserName, Password);
            }
        }

        private void promptForPassword()
        {
            Console.WriteLine("You have supplied a username only, please enter the password for accessing the protected feed:");
            Console.ResetColor();

            //from http://stackoverflow.com/a/3404522/1793
            var pass = string.Empty;
            ConsoleKeyInfo key;
            do
            {
                key = Console.ReadKey(true);

                // Backspace Should Not Work
                if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
                {
                    pass += key.KeyChar;
                    Console.Write("*");
                }
                else
                {
                    if (key.Key == ConsoleKey.Backspace && pass.Length > 0)
                    {
                        pass = pass.Substring(0, (pass.Length - 1));
                        Console.Write("\b \b");
                    }
                }
            }
            // Stops Receving Keys Once Enter is Pressed
            while (key.Key != ConsoleKey.Enter);
            Console.WriteLine();
            Console.WriteLine("Thank you.");
            Password = pass;
        }

        private void GenerateFile(List<PackageViewModel> packages, string fileName)
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

            if (WholeDiagramAndProjectDiagramsNotSpecified())
            {
                Console.WriteLine("You must specify either wholediagram and/or projectdiagrams.");
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

        private bool WholeDiagramAndProjectDiagramsNotSpecified()
        {
            return !(WholeDiagram || ProjectDiagrams);
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
            if (OutputPath != string.Empty) Directory.CreateDirectory(OutputPath);
            return Path.Combine(OutputPath, string.Format("{0}.{1}", name, GetFileExtension()));
        }

        private string GetFileExtension()
        {
            return OutputType == "graphviz" ? "dot" : OutputType;
        }
    }
}