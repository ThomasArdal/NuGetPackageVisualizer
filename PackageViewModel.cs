namespace NuGetPackageVisualizer
{
    public class PackageViewModel
    {
        public string NugetId { get; set; }

        public string Id { get; set; }

        public string RemoteVersion { get; set; }

        public string LocalVersion { get; set; }

        public DependencyViewModel[] Dependencies { get; set; }
    }
}