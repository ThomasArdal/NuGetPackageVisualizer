using System.Collections.Generic;
using System.Linq;

namespace NuGetPackageVisualizer
{
    public static class GraphHelper
    {
        public static string GenerateBackgroundColor(IEnumerable<PackageViewModel> packages, PackageViewModel package, IColorConfiguration colors)
        {
            if (VersionMismatch(package))
            {
                return colors.VersionMismatchPackageColor;
            }

            if (HasSamePackageInDifferentVersion(packages, package))
            {
                return colors.PackageHasDifferentVersionsColor;
            }

            return colors.DefaultColor;
        }

        private static bool HasSamePackageInDifferentVersion(IEnumerable<PackageViewModel> packages, PackageViewModel package)
        {
            return packages.Any(p => p.NugetId == package.NugetId && p.LocalVersion != package.LocalVersion);
        }

        private static bool VersionMismatch(PackageViewModel package)
        {
            return package.LocalVersion != package.RemoteVersion;
        }
    }
}