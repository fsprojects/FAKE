using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Fake;
using Microsoft.FSharp.Core;
using FSharp.Testing;
using Machine.Specifications;

namespace Test.FAKECore.SquirrelHelperSpec
{
    [Subject(typeof(Squirrel), "runner argument construction")]
    internal abstract class BuildArgumentsSpecsBase
    {
        protected static Squirrel.SquirrelParams Parameters;
        protected static string Arguments;
        protected static string NuGetPackage = "my.nuget";

        Establish context = () =>
        {
            Parameters = Squirrel.SquirrelDefaults;
        };

        Because of = () =>
        {
            Arguments = Squirrel.buildSquirrelArgs(Parameters, NuGetPackage);
            Console.WriteLine(Arguments);
        };
    }

    internal class When_using_the_default_parameters
        : BuildArgumentsSpecsBase
    {
        It should_include_releasify = () => Arguments.ShouldContain("--releasify=" + NuGetPackage);
        It should_not_include_releasedir = () => Arguments.ShouldNotContain("--releaseDir=");
        It should_not_include_loading_gif = () => Arguments.ShouldNotContain("--loadingGif=");
        It should_not_include_setup_icon = () => Arguments.ShouldNotContain("--setupIcon=");
        It should_not_include_nomsi = () => Arguments.ShouldNotContain("--no-msi");
        It should_not_include_bootstrapper_exe = () => Arguments.ShouldNotContain("--bootstrapperExe=");
    }

    internal class When_specifying_nuget_package
        : BuildArgumentsSpecsBase
    {
        It should_include_nuget_package = () => Arguments.ShouldContain("--releasify=" + NuGetPackage);
    }

    internal class When_specifying_release_dir
        : BuildArgumentsSpecsBase
    {
        const string ReleaseDir = "releasedir";
        Establish context = () => Parameters = Parameters.With(p => p.ReleaseDir, ReleaseDir);

        It should_include_release_dir = () => Arguments.ShouldContain("--releaseDir=" + ReleaseDir);
    }

    internal class When_specifying_loading_gif
        : BuildArgumentsSpecsBase
    {
        const string LoadingGif = "spinner.gif";
        Establish context = () => Parameters = Parameters.With(p => p.LoadingGif, FSharpOption<string>.Some(LoadingGif));

        It should_include_loading_gif_param = () => Arguments.ShouldContain("--loadingGif=" + LoadingGif);
    }

    internal class When_specifying_setup_icon
        : BuildArgumentsSpecsBase
    {
        const string SetupIcon = "setup.ico";
        Establish context = () => Parameters = Parameters.With(p => p.SetupIcon, FSharpOption<string>.Some(SetupIcon));

        It should_include_setup_icon = () => Arguments.ShouldContain("--setupIcon=" + SetupIcon);
    }

    internal class When_specifying_nomsi
        : BuildArgumentsSpecsBase
    {
        Establish context = () => Parameters = Parameters.With(p => p.NoMsi, true);

        It should_include_nomsi = () => Arguments.ShouldContain("--no-msi");
    }

    internal class When_specifying_bootstrapper_exe
        : BuildArgumentsSpecsBase
    {
        const string BootstrapperExe = "bootstrap.exe";
        Establish context = () => Parameters = Parameters.With(p => p.BootstrapperExe, FSharpOption<string>.Some(BootstrapperExe));

        It should_include_bootstrapper_param = () => Arguments.ShouldContain("--bootstrapperExe=" + BootstrapperExe);
    }

    internal class When_requesting_package_signing_with_default_parameters
        : BuildArgumentsSpecsBase
    {
        Establish context = () => Parameters = Parameters.With(p => p.SignExecutable, FSharpOption<bool>.Some(true));

        It should_include_signWithParams = () => Arguments.ShouldContain("--signWithParams=\"");
        It should_include_param_a = () => Arguments.ShouldContain("/a");
        It should_not_include_signing_key_file = () => Arguments.ShouldNotContain("/f");
        It should_not_include_signing_key_secret = () => Arguments.ShouldNotContain("/s");
    }

    internal class When_requesting_package_signing_with_signing_file
        : BuildArgumentsSpecsBase
    {
        const string SignKeyFile = "signing.pfx";
        Establish context = () => Parameters = Parameters
            .With(p => p.SignExecutable, FSharpOption<bool>.Some(true))
            .With(p => p.SigningKeyFile, FSharpOption<string>.Some(SignKeyFile));

        It should_include_signWithParams = () => Arguments.ShouldContain("--signWithParams=\"");
        It should_include_param_a = () => Arguments.ShouldContain("/a");
        It should_include_signing_key_file = () => Arguments.ShouldContain("/f " + SignKeyFile);
        It should_not_include_signing_key_secret = () => Arguments.ShouldNotContain("/s");
    }

    internal class When_requesting_package_signing_with_signing_secret
        : BuildArgumentsSpecsBase
    {
        const string SignSecret = "mysecret";
        Establish context = () => Parameters = Parameters
            .With(p => p.SignExecutable, FSharpOption<bool>.Some(true))
            .With(p => p.SigningSecret, FSharpOption<string>.Some(SignSecret));

        It should_include_signWithParams = () => Arguments.ShouldContain("--signWithParams=\"");
        It should_include_param_a = () => Arguments.ShouldContain("/a");
        It should_not_include_signing_key_file = () => Arguments.ShouldNotContain("/f ");
        It should_include_signing_key_secret = () => Arguments.ShouldContain("/p " + SignSecret);
    }

    internal class When_requesting_package_signing_with_parameters
        : BuildArgumentsSpecsBase
    {
        const string SignKeyFile = "signing.pfx";
        const string SignSecret = "mysecret";
        Establish context = () => Parameters = Parameters
            .With(p => p.SignExecutable, FSharpOption<bool>.Some(true))
            .With(p => p.SigningKeyFile, FSharpOption<string>.Some(SignKeyFile))
            .With(p => p.SigningSecret, FSharpOption<string>.Some(SignSecret));

        It should_include_signWithParams = () => Arguments.ShouldContain("--signWithParams=\"");
        It should_include_param_a = () => Arguments.ShouldContain("/a");
        It should_include_signing_key_file = () => Arguments.ShouldContain("/f " + SignKeyFile);
        It should_include_signing_key_secret = () => Arguments.ShouldContain("/p " + SignSecret);
    }
}