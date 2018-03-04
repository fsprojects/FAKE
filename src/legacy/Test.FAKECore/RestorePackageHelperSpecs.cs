using System;
using System.IO;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Collections;
using Microsoft.FSharp.Core;

namespace Test.FAKECore
{
    public class when_restoring_package_by_id
    {
        static readonly TimeSpan OneMinute = new TimeSpan(0, 1, 0);

        It should_restore_package_by_package_id_with_ExcludeVersion =
            () =>
            {
                var packageParams = new RestorePackageHelper.RestoreSinglePackageParams(
                    "NuGet.exe",
                    FSharpList<string>.Empty,
                    OneMinute,
                    "./myPackageFolder/",
                    null,
                    true,
                    5,
                    false);
                var result = Run(packageParams);
                result.ShouldStartWith(" \"install\" \"thePackage\" \"-OutputDirectory\" \"");
                result.ShouldEndWith(string.Format("{0}test{0}myPackageFolder{0}\" \"-ExcludeVersion\"", Path.DirectorySeparatorChar));
            };

        It should_restore_package_by_package_id_with_ExcludeVersion_and_PreReleasePackage =
            () =>
            {
                var packageParams = new RestorePackageHelper.RestoreSinglePackageParams(
                    "NuGet.exe",
                    FSharpList<string>.Empty,
                    OneMinute,
                    "./myPackageFolder/",
                    new FSharpOption<Version>(new Version("1.2.3.4")),
                    true,
                    5,
                    true);
                var result = Run(packageParams);
                result.ShouldStartWith(" \"install\" \"thePackage\" \"-OutputDirectory\" \"");
                result.ShouldEndWith(string.Format("{0}test{0}myPackageFolder{0}\" \"-ExcludeVersion\" \"-PreRelease\"", Path.DirectorySeparatorChar));
            };

        It should_restore_package_by_package_id_with_PreReleasePackage =
            () =>
            {
                var packageParams = new RestorePackageHelper.RestoreSinglePackageParams(
                    "NuGet.exe",
                    FSharpList<string>.Empty,
                    OneMinute,
                    "./myPackageFolder/",
                    null,
                    false,
                    5,
                    true);
                var result = Run(packageParams);
                result.ShouldStartWith(" \"install\" \"thePackage\" \"-OutputDirectory\" \"");
                result.ShouldEndWith(string.Format("{0}test{0}myPackageFolder{0}\" \"-PreRelease\"", Path.DirectorySeparatorChar));
            };

        It should_restore_package_by_package_id_with_Version =
            () =>
            {
                var packageParams = new RestorePackageHelper.RestoreSinglePackageParams(
                    "NuGet.exe",
                    FSharpList<string>.Empty,
                    OneMinute,
                    "./myPackageFolder/",
                    new FSharpOption<Version>(new Version("1.2.3.4")),
                    false,
                    5,
                    false);
                var result = Run(packageParams);
                result.ShouldStartWith(" \"install\" \"thePackage\" \"-OutputDirectory\" \"");
                result.ShouldEndWith(string.Format("{0}test{0}myPackageFolder{0}\" \"-Version\" \"1.2.3.4\"", Path.DirectorySeparatorChar));
            };

        It should_restore_package_by_package_id_with_Version_and_ExcludeVersion =
            () =>
            {
                var packageParams = new RestorePackageHelper.RestoreSinglePackageParams(
                    "NuGet.exe",
                    FSharpList<string>.Empty,
                    OneMinute,
                    "./myPackageFolder/",
                    new FSharpOption<Version>(new Version("1.2.3.4")),
                    true,
                    5,
                    false);
                var result = Run(packageParams);
                result.ShouldStartWith(" \"install\" \"thePackage\" \"-OutputDirectory\" \"");
                result.ShouldEndWith(string.Format("{0}test{0}myPackageFolder{0}\" \"-ExcludeVersion\" \"-Version\" \"1.2.3.4\"", Path.DirectorySeparatorChar));
            };

        It should_restore_package_by_package_id_with_defaults =
            () =>
            {
                var packageParams = RestorePackageHelper.RestoreSinglePackageDefaults;
                var result = Run(packageParams);
                result.ShouldStartWith(" \"install\" \"thePackage\" \"-OutputDirectory\" \"");
                result.ShouldEndWith(string.Format("{0}packages\"", Path.DirectorySeparatorChar));
            };

        static string Run(RestorePackageHelper.RestoreSinglePackageParams packageParams)
        {
            Func<RestorePackageHelper.RestoreSinglePackageParams, RestorePackageHelper.RestoreSinglePackageParams> f = _ => packageParams;
            var fs = FuncConvert.ToFSharpFunc(new Converter<RestorePackageHelper.RestoreSinglePackageParams, RestorePackageHelper.RestoreSinglePackageParams>(f));
            var result = RestorePackageHelper.buildNuGetArgs(fs, "thePackage");
            return result;
        }
    }
}
