namespace NuGetPackageVisualizer
{
    public interface IColorConfiguration
    {
        string VersionMismatchPackageColor { get; }
        string PackageHasDifferentVersionsColor { get; }
        string DefaultColor { get; }
    }
}