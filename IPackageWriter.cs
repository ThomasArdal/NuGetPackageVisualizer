using System.Collections.Generic;

namespace NuGetPackageVisualizer
{
    public interface IPackageWriter
    {
        void Write(List<PackageViewModel> packages, string file);
    }
}