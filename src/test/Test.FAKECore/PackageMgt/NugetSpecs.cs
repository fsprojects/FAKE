using System.IO;
using System.Linq;
using Fake;
using Machine.Specifications;
using Microsoft.FSharp.Core;
using Microsoft.FSharp.Collections;

namespace Test.FAKECore.PackageMgt
{
    public class when_packing_with_nuspec_template
    {
        static string tempDir, pkgFile, nuspecFile;
        static FSharpFunc<NuGetHelper.NuGetParams, NuGetHelper.NuGetParams> nugetParams;

        Establish context = () =>
        {
            tempDir = Path.GetTempPath();
            pkgFile = Path.Combine(tempDir, "fake.0.0.1.nupkg");
            nuspecFile = Path.Combine(TestData.TestDataDir, "fake.nuspec");

            nugetParams = FSharpFuncUtil.ToFSharpFunc<NuGetHelper.NuGetParams, NuGetHelper.NuGetParams>(
                p => {
                    return new NuGetHelper.NuGetParams(
                        Authors: ListModule.OfSeq(new [] { "author" }),
                        Project: "fake",
                        Description: "decription",
                        OutputPath: tempDir,
                        Summary: "summary",
                        WorkingDir: TestData.TestDataDir,
                        Version: "0.0.1",

                        Files: p.Files,

                        AccessKey: p.AccessKey,
                        Copyright: p.Copyright,
                        Dependencies: p.Dependencies,
                        DependenciesByFramework: p.DependenciesByFramework,
                        IncludeReferencedProjects: p.IncludeReferencedProjects,
                        NoPackageAnalysis: p.NoPackageAnalysis,
                        ProjectFile: p.ProjectFile,
                        Properties: p.Properties,
                        Publish: p.Publish,
                        PublishTrials: p.PublishTrials,
                        PublishUrl: p.PublishUrl,
                        References: p.References,
                        ReferencesByFramework: p.ReferencesByFramework,
                        ReleaseNotes: p.ReleaseNotes,
                        SymbolPackage: p.SymbolPackage,
                        Tags: p.Tags,
                        TimeOut: p.TimeOut,
                        Title: p.Title,
                        ToolPath: p.ToolPath
                    );
                }
            );
        };

        Because of = () => NuGetHelper.NuGetPack(nugetParams, nuspecFile);
        It should_create_nupkg_file = () => File.Exists(pkgFile).ShouldBeTrue();
    }
}
