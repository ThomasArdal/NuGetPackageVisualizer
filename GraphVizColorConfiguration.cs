namespace NuGetPackageVisualizer
{
    public class GraphVizColorConfiguration : IColorConfiguration
    {
        public string VersionMismatchPackageColor
        {
            get { return "red"; }
        }

        public string PackageHasDifferentVersionsColor
        {
            get { return "grey"; }
        }

        public string DefaultColor
        {
            get { return "forestgreen"; }
        }
    }
}