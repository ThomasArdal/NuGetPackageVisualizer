namespace NuGetPackageVisualizer
{
    public class DgmlColorConfiguration : IColorConfiguration
    {
        public string VersionMismatchPackageColor
        {
            get { return "#FF0000"; }
        }

        public string PackageHasDifferentVersionsColor
        {
            get { return "#FCE428"; }
        }

        public string DefaultColor
        {
            get { return "#15FF00"; }
        }
    }
}