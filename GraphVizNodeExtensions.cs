namespace NuGetPackageVisualizer
{
    public static class GraphVizNodeExtensions
    {
        public static string GraphId(this PackageViewModel model)
        {
            return string.Format("{0}_{1}", model.NugetId, model.LocalVersion);            
        }

        public static string DisplayVersion(this PackageViewModel model)
        {
            return string.Format("{0} ({1})", model.NugetId, model.LocalVersion);
        }

        public static string GraphId(this DependencyViewModel model)
        {
            return string.Format("{0}_{1}", model.NugetId, model.Version);            
        }

        public static string DisplayVersion(this DependencyViewModel model)
        {
            return string.Format("{0} ({1})", model.NugetId, model.Version);            
        }
    }
}