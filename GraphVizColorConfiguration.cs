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
            get { return "forestgreen"; }
        }

        public string DefaultColor
        {
            get { return "white"; }
        }
    }
}